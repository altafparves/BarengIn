# 0001. Domain layer infrastructure boundary

## Context

Task A5 implemented `BarengIn.Domain` (`src/BarengIn.Domain`) directly from the Module 3 class
diagram (see `README.md`, "Module 3 — Class Design"). `BarengIn.Domain` must stay 100%
dependency-free: no EF Core, no project references, no external NuGet packages, plain C# only.

Several methods in the diagram — `User.Login`, `Passenger.SearchTrips`, `Admin.VerifyKtm`,
`Vehicle.GetEmissionFactor`, and others — describe behaviour that genuinely needs something the
Domain layer cannot have: a password hashing algorithm, a repository query across many entities,
or a lookup by id when the entity holding the id has no reference to look through.

Separately, several relationships in the diagram (`User *-- ClassSchedule`, `Driver *-- Vehicle`,
`Driver o-- Trip`, `Trip o-- RideRequest`, `Passenger o-- RideRequest`) show composition or
aggregation but the diagram's attribute lists don't include a backing field for them.

This record exists so Calvin (backend, building the Application layer next) doesn't mistake either
pattern below for a bug.

## Decision 1: infrastructure-bound methods throw `NotImplementedException`

A method whose diagram signature cannot be satisfied with data the entity itself holds throws
`NotImplementedException`, with a message naming what's missing and which layer resolves it.
Examples: `User.Login` (needs a hashing algorithm), `Passenger.SearchTrips` (needs a repository
query), `Admin.VerifyKtm` (needs a `User` lookup by `Guid`), `Vehicle.GetEmissionFactor` and
`Trip.CalculateCo2Saved` (need an `EmissionFactor` lookup by `VehicleType`), `Driver.PublishTrip`
(the signature carries only a vehicle id, departure time and seat count — not the origin,
destination or route a valid `Trip` needs), `RideRequest.GetDetourDistance` (needs the destination
Faculty's `GeoPoint`, and `Trip` only stores a destination faculty id).

Everything else — state transitions (`Trip.Start`/`Complete`/`Cancel`, `RideRequest.Approve`/
`Reject`/`Cancel`), pure calculations (`GeoPoint.DistanceTo`, `Money.Split`,
`EmissionFactor.CalculateEmission`, `Trip.CalculateCostPerSeat`), and everything reachable through
the collections in Decision 2 (`Driver.ViewIncomingRequests`/`ApproveRequest`/`RejectRequest`/
`CompleteTrip`) — is implemented with real logic, not a stub.

**Consequence:** the Application layer must catch these on the seam and provide the missing piece
(a repository, a hasher, a richer command object) rather than expecting a return value.

## Decision 2: composition/aggregation relationships get real backing collections

Where the diagram shows composition or aggregation but no attribute (e.g. `Driver o-- Trip:
publishes`), the owning class gets a private `List<T>` and a small accessor method to populate it
— e.g. `Driver.RecordPublishedTrip(Trip trip)`, `Passenger.RecordSubmittedRequest(RideRequest
request)`, `Trip.ReceiveRequest(RideRequest request)`, `User.AddClassSchedule(ClassSchedule
schedule)`. These accessor methods are **not** in the diagram's method list; they exist only so
the relationship is a real, traversable collection instead of an implied one nothing can populate.

Read access is exposed as `IReadOnlyList<T>` properties (e.g. `Driver.PublishedTrips`,
`Trip.RideRequests`).

One relationship already had a diagram method to populate it: `Driver.AddVehicle` backs `Driver
*-- Vehicle: owns` directly, no extra accessor needed.

**Consequence:** because `Driver.PublishTrip` throws (Decision 1), nothing in the Domain layer
populates `Driver.PublishedTrips` on its own. The Application layer is expected to construct a
`Trip` directly (Trip's constructor is public and fully validated) once it has resolved
origin/destination/route data, then call `Driver.RecordPublishedTrip` to keep the aggregate
consistent. The same pattern applies to `Passenger.RequestRide` and `RecordSubmittedRequest`.
