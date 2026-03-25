using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Interfaces;

namespace CoreService.Domain.Entities;

public class Internship
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
    
    public async Task OfferPositionAsync(
        InternshipApplication internshipApplication,
        IInternshipCapacityChecker capacityChecker,
        CancellationToken cancellationToken = default)
    {
        if (internshipApplication.Internship.Id != Id)
            throw new ApplicationMismatchException("This application does not belong to the current internship.");

        var reservedSpots = await capacityChecker.CountReservedSpotsAsync(Id, cancellationToken);

        if (reservedSpots >= Capacity)
            throw new CapacityExceededException($"Internship capacity of {Capacity} has been reached.");

        internshipApplication.MarkAsAccepted();
    }
}