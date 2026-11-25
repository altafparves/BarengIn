using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain;

/// <summary>
/// A passenger who also offers trips as a driver.
/// Demonstrates OOP Inheritance (User -> Passenger -> Driver), Polymorphism (overrides CanPublishTrip
/// and GetRoleName), and Encapsulation with private backing collections (vehicles, publishedTrips).
/// </summary>
public class Driver : Passenger
{
    private string licenseNumber;
    private double rating;
    private int totalTripsCompleted;

    // Access Modifier (private readonly): internal collections cannot be mutated outside Driver.
    private readonly List<Vehicle> vehicles = [];
    private readonly List<Trip> publishedTrips = [];

    public Driver(
        string niu,
        string fullName,
        string email,
        string passwordHash,
        string phoneNumber,
        Guid facultyId,
        string homeAddress,
        GeoPoint homeLocation,
        string licenseNumber)
        : base(niu, fullName, email, passwordHash, phoneNumber, facultyId, homeAddress, homeLocation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);

        this.licenseNumber = licenseNumber;
        rating = 0;
        totalTripsCompleted = 0;
    }

    /// <summary>Vehicles this driver owns, exposed read-only (Encapsulation).</summary>
    public IReadOnlyList<Vehicle> Vehicles => vehicles;

    /// <summary>Trips this driver has published, exposed read-only (Encapsulation).</summary>
    public IReadOnlyList<Trip> PublishedTrips => publishedTrips;

    /// <summary>Polymorphism: overrides Passenger.CanPublishTrip() (which returns false).</summary>
    public override bool CanPublishTrip() => IsVerified() && vehicles.Count > 0;

    /// <summary>Polymorphism: overrides User.GetRoleName() for Driver accounts.</summary>
    public override string GetRoleName() => "Driver";

    public bool AddVehicle(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        vehicles.Add(vehicle);
        return true;
    }

    public Trip PublishTrip(Guid vehicleId, DateTime departure, int seats) =>
        throw new NotImplementedException(
            "The diagram signature lacks origin/destination/route data; the Application layer constructs the Trip.");

    /// <summary>
    /// Publishes a new trip after verifying driver eligibility via polymorphic <see cref="CanPublishTrip"/>.
    /// Encapsulates Trip creation and adds it to the private <c>publishedTrips</c> collection.
    /// </summary>
    public Trip PublishNewTrip(
        Guid vehicleId,
        string originAddress,
        GeoPoint originLocation,
        Guid destinationFacultyId,
        DateTime departureTime,
        int seats,
        decimal costPerSeatIdr)
    {
        if (!CanPublishTrip())
        {
            throw new InvalidOperationException(
                "Driver must be verified and have a registered vehicle to publish a trip.");
        }

        var trip = new Trip(
            userId,
            vehicleId,
            originAddress,
            originLocation,
            destinationFacultyId,
            departureTime,
            seats,
            distanceKm: 5.0,
            polylineJson: string.Empty);

        trip.SetCostPerSeat(new Money(costPerSeatIdr, "IDR"));
        publishedTrips.Add(trip);
        return trip;
    }

    public List<RideRequest> ViewIncomingRequests(Guid tripId)
    {
        var trip = publishedTrips.FirstOrDefault(t => t.TripId == tripId)
            ?? throw new ArgumentException(
                "Trip not found among this driver's published trips.", nameof(tripId));

        return [.. trip.RideRequests];
    }

    /// <summary>
    /// Encapsulation: validates pending state and seat availability before seating passenger
    /// and transitioning RideRequest status to Approved.
    /// </summary>
    public bool ApproveRequest(Guid requestId)
    {
        var (trip, request) = FindRequest(requestId);
        if (trip is null || request is null || !request.IsPending() || !trip.HasAvailableSeat())
        {
            return false;
        }

        if (!trip.AddPassenger(request.PassengerId))
        {
            return false;
        }

        return request.Approve();
    }

    public bool RejectRequest(Guid requestId, string reason)
    {
        var (_, request) = FindRequest(requestId);
        return request is not null && request.Reject(reason);
    }

    public bool CompleteTrip(Guid tripId)
    {
        var trip = publishedTrips.FirstOrDefault(t => t.TripId == tripId);
        if (trip is null || !trip.Complete())
        {
            return false;
        }

        totalTripsCompleted++;
        return true;
    }

    public void RecordPublishedTrip(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        publishedTrips.Add(trip);
    }

    private (Trip? Trip, RideRequest? Request) FindRequest(Guid requestId)
    {
        foreach (var trip in publishedTrips)
        {
            var request = trip.RideRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (request is not null)
            {
                return (trip, request);
            }
        }

        return (null, null);
    }
}
