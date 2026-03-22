using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;
using InternshipTracker.Domain.Interfaces;

namespace InternshipTracker.Domain.Entities;

public class Internship : IEntity
{
    public Internship(Guid id, string title, int capacity, CandidateLevel minimumLevel)
    {
        Id = id;
        Title = title;
        Capacity = capacity;
        MinimumLevel = minimumLevel;
    }

    public string Title { get; private set; }
    public int Capacity { get; }
    public CandidateLevel MinimumLevel { get; private set; }
    public Guid Id { get; init; }

    // Capacity Limit
    public async Task OfferPositionAsync(
        InternshipApplication application,
        IInternshipCapacityChecker capacityChecker,
        CancellationToken cancellationToken = default)
    {
        if (application.Internship.Id != Id)
            throw new ApplicationMismatchException("This application does not belong to the current internship.");

        var reservedSpots = await capacityChecker.CountReservedSpotsAsync(Id, cancellationToken);

        if (reservedSpots >= Capacity)
            throw new CapacityExceededException($"Internship capacity of {Capacity} has been reached.");

        application.MarkAsAccepted();
    }
}