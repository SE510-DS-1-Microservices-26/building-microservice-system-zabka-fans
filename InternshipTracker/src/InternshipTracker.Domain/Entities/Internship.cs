using System.Net.Mime;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;
using InternshipTracker.Domain.Interfaces;

namespace InternshipTracker.Domain.Entities;

public class Internship : IEntity
{
    public Guid Id { get; init; }
    public string Title { get; private set; }
    public int Capacity { get; private set; }
    public CandidateLevel MinimumLevel { get; private set; }
    
    public Internship(Guid id, string title, int capacity, CandidateLevel minimumLevel)
    {
        Id = id;
        Title = title;
        Capacity = capacity;
        MinimumLevel = minimumLevel;
    }

    // Capacity Limit
    public async Task OfferPositionAsync(
        InternshipApplication application, 
        IInternshipCapacityChecker capacityChecker,
        CancellationToken cancellationToken = default)
    {
        if (application.Internship.Id != this.Id)
            throw new ApplicationMismatchException("This application does not belong to the current internship.");
        
        int reservedSpots = await capacityChecker.CountReservedSpotsAsync(this.Id, cancellationToken);

        if (reservedSpots >= Capacity)
            throw new CapacityExceededException($"Internship capacity of {Capacity} has been reached.");

        application.MarkAsAccepted();
    }
}