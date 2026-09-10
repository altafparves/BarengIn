# BarengIn
Social carpooling for university students, matched by class schedule. Junior Project (OOP & Databases), DTETI FT UGM.

## About the Project
BarengIn is a carpooling and ride sharing platform built specifically for university students in Indonesia. By matching students' class schedules and destination faculties.

The app connects **Drivers** (students who already commute with a private vehicle and have empty seats) with **Passengers** (students without a vehicle who need a reliable, precisely timed ride to campus). Every user verifies their identity with their student ID (KTM), ensuring a safer and more trustworthy environment than conventional ride hailing apps.
Beyond just transportation, BarengIn layers in _social profiles_, such as hobbies and short bios so that daily commutes become an opportunity to meet like minded peers, giving drivers full control to approve or decline ride requests with zero pressure from the system.

The platform was created to address three interconnected problems on Indonesian campuses: chronic traffic congestion at campus gates driven by single occupancy commuting, the financial burden of daily transportation for students without vehicles, and the untapped potential for social connection during otherwise mundane commutes.

## Problem Statement
- Traffic congestion at campus gates caused by single occupancy commuting
- Unaffordable or unscheduled transport options for students without a vehicle
- Uneven cost burden on students who do own a vehicle
- Low social interaction during daily commutes
- No visibility into commute related carbon emissions

## Key Features
- Student ID (KTM) verification
- Automatic schedule and route matching between drivers and passengers
- Real time GPS tracking during a trip
- Social profiles with approve/reject matching
- Flexible, driver set cost sharing
- Admin dashboard for reports and CO2 savings analytics

## Climate Action Alignment
BarengIn reduces vehicle trips per capita by consolidating single occupancy commutes into shared rides, and gives campus administrators visibility into estimated emissions reduced through the admin dashboard.

## Team

BarengIn

- Member 1: Altaf Parves Shua Ilham – 24/536741/TK/59565
- Member 2: Calvin
- Member 3: M Dimas Dwi Ananda
- Member 4: Diffie Alfierie Iswanto

| Role | Name | NIU |
|---|---|---|
| Software Architect | Altaf Parves Shua Ilham | 24/536741/TK/59565 |
| Backend Engineer | Calvin | 24/532894/TK/59025 |
| Frontend Engineer | M Dimas Dwi Ananda | 24/536904/TK/59594 |
| Frontend Engineer | Diffie Alfierie Iswanto | 24/533049/TK/59056 |


## Tech Stack
| Layer | Technology |
|---|---|
| Platform | Platform (web vs. mobile, decision pending) |
| Backend | C#, ASP.NET Core 8 Web API |
| Database | PostgreSQL, Entity Framework Core |
| Real time | SignalR |
| Frontend | TBD, pending platform decision |

## Similar Apps
Gojek/GoCar, Grab, and BlaBlaCar are the closest comparisons. BarengIn differentiates on schedule based matching, social profiles, and a campus specific climate impact dashboard.

## Project Status
In development. Module 1 (concept, repo setup, GitHub Page) is in progress, with architecture and API design next.

## Module 3 — Class Design

```mermaid
classDiagram
    namespace Enumerations {
        class VerificationStatus {
            <<enumeration>>
            Unverified
            Pending
            Verified
            Rejected
        }
        class TripStatus {
            <<enumeration>>
            Draft
            Published
            Full
            InProgress
            Completed
            Cancelled
        }
        class RequestStatus {
            <<enumeration>>
            Pending
            Approved
            Rejected
            Cancelled
        }
        class VehicleType {
            <<enumeration>>
            Motorcycle
            Car
        }
    }

    namespace ValueObjects {
        class GeoPoint {
            <<value object>>
            -latitude : double
            -longitude : double
            +GeoPoint(lat : double, lng : double)
            +DistanceTo(other : GeoPoint) double
            +IsWithinRadius(other : GeoPoint, km : double) bool
            +ToString() string
        }
        class Money {
            <<value object>>
            -amount : decimal
            -currency : string
            +Money(amount : decimal, currency : string)
            +Add(other : Money) Money
            +Split(parts : int) Money
            +ToString() string
        }
    }

    class User {
        <<abstract>>
        #userId : Guid
        #niu : string
        #fullName : string
        #email : string
        #passwordHash : string
        #phoneNumber : string
        #ktmImageUrl : string
        #verificationStatus : VerificationStatus
        #facultyId : Guid
        #hobbies : string
        #bio : string
        #createdAt : DateTime
        +Login(email : string, password : string) bool
        +Logout() void
        +UpdateProfile(name : string, phone : string, bio : string) bool
        +UploadKtm(imageUrl : string) bool
        +IsVerified() bool
        +CanPublishTrip()* bool
        +GetRoleName()* string
    }

    class Passenger {
        -homeAddress : string
        -homeLocation : GeoPoint
        -currentLocation : GeoPoint
        +CanPublishTrip() bool
        +GetRoleName() string
        +UpdateCurrentLocation(point : GeoPoint) void
        +SearchTrips(date : DateTime, facultyId : Guid) List~Trip~
        +RequestRide(tripId : Guid, pickup : GeoPoint) RideRequest
        +CancelRequest(requestId : Guid) bool
        +GetTripHistory() List~Trip~
    }

    class Driver {
        -licenseNumber : string
        -rating : double
        -totalTripsCompleted : int
        +CanPublishTrip() bool
        +GetRoleName() string
        +AddVehicle(vehicle : Vehicle) bool
        +PublishTrip(vehicleId : Guid, departure : DateTime, seats : int) Trip
        +ViewIncomingRequests(tripId : Guid) List~RideRequest~
        +ApproveRequest(requestId : Guid) bool
        +RejectRequest(requestId : Guid, reason : string) bool
        +CompleteTrip(tripId : Guid) bool
    }

    class Admin {
        -department : string
        -adminLevel : int
        +CanPublishTrip() bool
        +GetRoleName() string
        +VerifyKtm(userId : Guid, status : VerificationStatus) bool
        +SuspendUser(userId : Guid, reason : string) bool
        +ViewEmissionDashboard(from : DateTime, to : DateTime) double
        +GenerateReport(from : DateTime, to : DateTime) string
    }

    class Vehicle {
        -vehicleId : Guid
        -driverId : Guid
        -plateNumber : string
        -brand : string
        -model : string
        -color : string
        -type : VehicleType
        -seatCapacity : int
        +GetSeatCapacity() int
        +UpdateDetails(brand : string, model : string, color : string) bool
        +GetEmissionFactor() EmissionFactor
    }

    class Trip {
        -tripId : Guid
        -driverId : Guid
        -vehicleId : Guid
        -originAddress : string
        -originLocation : GeoPoint
        -destinationFacultyId : Guid
        -departureTime : DateTime
        -availableSeats : int
        -costPerSeat : Money
        -distanceKm : double
        -polylineJson : string
        -status : TripStatus
        -createdAt : DateTime
        +HasAvailableSeat() bool
        +AddPassenger(passengerId : Guid) bool
        +RemovePassenger(passengerId : Guid) bool
        +CalculateCostPerSeat(fuelPrice : decimal) Money
        +CalculateCo2Saved() double
        +Start() bool
        +Complete() bool
        +Cancel(reason : string) bool
        +IsMatchingSchedule(schedule : ClassSchedule) bool
    }

    class RideRequest {
        -requestId : Guid
        -tripId : Guid
        -passengerId : Guid
        -pickupAddress : string
        -pickupPoint : GeoPoint
        -message : string
        -status : RequestStatus
        -requestedAt : DateTime
        -respondedAt : DateTime
        +Approve() bool
        +Reject(reason : string) bool
        +Cancel() bool
        +IsPending() bool
        +GetDetourDistance(trip : Trip) double
    }

    class ClassSchedule {
        -scheduleId : Guid
        -userId : Guid
        -courseName : string
        -facultyId : Guid
        -dayOfWeek : DayOfWeek
        -startTime : TimeSpan
        -endTime : TimeSpan
        +OverlapsWith(other : ClassSchedule) bool
        +GetArrivalDeadline(date : DateTime) DateTime
        +MatchesTrip(trip : Trip) bool
    }

    class Faculty {
        -facultyId : Guid
        -name : string
        -shortName : string
        -location : GeoPoint
        +DistanceTo(point : GeoPoint) double
        +GetFullName() string
    }

    class EmissionFactor {
        -emissionFactorId : Guid
        -vehicleType : VehicleType
        -gramsCo2PerKm : double
        -source : string
        +CalculateEmission(distanceKm : double) double
        +CalculateSaving(distanceKm : double, occupants : int) double
    }

    User <|-- Passenger
    Passenger <|-- Driver
    User <|-- Admin

    User "1" *-- "0..*" ClassSchedule : owns
    User "0..*" --> "1" Faculty : belongs to

    Driver "1" *-- "1..*" Vehicle : owns
    Driver "1" o-- "0..*" Trip : publishes
    Vehicle "1" --> "0..*" Trip : used in

    Trip "1" o-- "0..*" RideRequest : receives
    Passenger "1" o-- "0..*" RideRequest : submits
    Trip "0..*" --> "1" Faculty : destination

    Vehicle "0..*" --> "1" EmissionFactor : rated by

    User ..> VerificationStatus
    Trip ..> TripStatus
    RideRequest ..> RequestStatus
    Vehicle ..> VehicleType

    User *-- GeoPoint
    Trip *-- GeoPoint
    RideRequest *-- GeoPoint
    Faculty *-- GeoPoint
    Trip *-- Money
```
