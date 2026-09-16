using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain;

/// <summary>A passenger who also offers trips as a driver.</summary>
public class Driver : Passenger
{
    private string licenseNumber;
    private double rating;
    private int totalTripsCompleted;

    /// <summary>Backs the "Driver owns Vehicle" composition; populated through <see cref="AddVehicle"/>.</summary>
    private readonly List<Vehicle> vehicles = [];

    /// <summary>Backs the "Driver publishes Trip" aggregation; not an attribute in the diagram.</summary>
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

    /// <summary>Vehicles this driver owns, exposed read-only.</summary>
    public IReadOnlyList<Vehicle> Vehicles => vehicles;

    /// <summary>Trips this driver has published, exposed read-only.</summary>
    public IReadOnlyList<Trip> PublishedTrips => publishedTrips;

    /// <summary>A driver may publish once verified and with at least one registered vehicle.</summary>
    public override bool CanPublishTrip() => IsVerified() && vehicles.Count > 0;

    public override string GetRoleName() => "Driver";

    public bool AddVehicle(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        vehicles.Add(vehicle);
        return true;
    }

    /// <summary>
    /// Not implemented here: the diagram signature carries only a vehicle id, a departure time and
    /// a seat count, not the origin, destination or route a valid <see cref="Trip"/> needs. The
    /// Application layer gathers that data and constructs the Trip directly, then calls
    /// <see cref="RecordPublishedTrip"/> to keep this aggregate consistent.
    /// </summary>
    public Trip PublishTrip(Guid vehicleId, DateTime departure, int seats) =>
        throw new NotImplementedException(
            "The diagram signature lacks origin/destination/route data; the Application layer constructs the Trip.");

    public List<RideRequest> ViewIncomingRequests(Guid tripId)
    {
        var trip = publishedTrips.FirstOrDefault(t => t.TripId == tripId)
            ?? throw new ArgumentException("Trip not found among this driver's published trips.", nameof(tripId));

        return [.. trip.RideRequests];
    }

    public bool ApproveRequest(Guid requestId)
    {
        var (trip, request) = FindRequest(requestId);
        if (trip is null || request is null)
        {
            return false;
        }

        return request.Approve() && trip.AddPassenger(request.PassengerId);
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

    /// <summary>
    /// Registers a trip this driver has published. Not a diagram method: it exists to make the
    /// "Driver publishes Trip" aggregation a real collection instead of an unreachable one.
    /// </summary>
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
