using BarengIn.Domain.ValueObjects;

namespace BarengIn.Domain;

/// <summary>A user looking for a ride. Also the base role for <see cref="Driver"/>.</summary>
public class Passenger : User
{
    private string homeAddress;
    private GeoPoint homeLocation;
    private GeoPoint currentLocation;

    /// <summary>Backs the "Passenger submits RideRequest" composition; not an attribute in the diagram.</summary>
    private readonly List<RideRequest> submittedRequests = [];

    public Passenger(
        string niu,
        string fullName,
        string email,
        string passwordHash,
        string phoneNumber,
        Guid facultyId,
        string homeAddress,
        GeoPoint homeLocation)
        : base(niu, fullName, email, passwordHash, phoneNumber, facultyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(homeAddress);
        ArgumentNullException.ThrowIfNull(homeLocation);

        this.homeAddress = homeAddress;
        this.homeLocation = homeLocation;
        currentLocation = homeLocation;
    }

    /// <summary>Requests this passenger has submitted, exposed read-only.</summary>
    public IReadOnlyList<RideRequest> SubmittedRequests => submittedRequests;

    /// <summary>A plain passenger never drives; only <see cref="Driver"/> overrides this to true.</summary>
    public override bool CanPublishTrip() => false;

    public override string GetRoleName() => "Passenger";

    public void UpdateCurrentLocation(GeoPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        currentLocation = point;
    }

    /// <summary>Not implemented here: searching trips by date and faculty needs a repository query.</summary>
    public List<Trip> SearchTrips(DateTime date, Guid facultyId) =>
        throw new NotImplementedException("Requires a Trip repository query, resolved in the Application layer.");

    /// <summary>
    /// Not implemented here: resolving <paramref name="tripId"/> to a <see cref="Trip"/> needs a
    /// repository lookup. Once the Application layer has the Trip, it constructs the RideRequest
    /// and calls <see cref="RecordSubmittedRequest"/> to keep this aggregate consistent.
    /// </summary>
    public RideRequest RequestRide(Guid tripId, GeoPoint pickup) =>
        throw new NotImplementedException("Requires a Trip repository lookup by id, resolved in the Application layer.");

    /// <summary>Not implemented here: resolving <paramref name="requestId"/> to a RideRequest needs a repository lookup.</summary>
    public bool CancelRequest(Guid requestId) =>
        throw new NotImplementedException("Requires a RideRequest repository lookup by id, resolved in the Application layer.");

    /// <summary>Not implemented here: trip history needs a repository query across completed trips.</summary>
    public List<Trip> GetTripHistory() =>
        throw new NotImplementedException("Requires a Trip repository query, resolved in the Application layer.");

    /// <summary>
    /// Records a request this passenger has submitted. Not a diagram method: it exists to make
    /// the "Passenger submits RideRequest" composition a real collection instead of an unreachable one.
    /// </summary>
    public void RecordSubmittedRequest(RideRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        submittedRequests.Add(request);
    }
}
