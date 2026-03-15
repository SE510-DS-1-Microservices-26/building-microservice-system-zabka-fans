using System.Net.Mime;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;

namespace InternshipTracker.Domain.Entities;

public class Internship : IEntity
{
    public Guid Id { get; init; }
    public string Title { get; private set; }
    public int Capacity { get; private set; }
    public CandidateLevel MinimumLevel { get; private set; }

    private readonly List<InternshipApplication> _applications = new();
    public IReadOnlyCollection<InternshipApplication> Applications => _applications.AsReadOnly();

    public Internship(Guid id, string title, int capacity, CandidateLevel minimumLevel)
    {
        Id = id;
        Title = title;
        Capacity = capacity;
        MinimumLevel = minimumLevel;
    }

    // Qualification Match
    public InternshipApplication ReceiveApplication(User candidate)
    {
        if (candidate.Level < MinimumLevel)
            throw new UnderqualifiedException($"Candidate level '{candidate.Level}' does not meet the minimum requirement of '{MinimumLevel}'.");

        var application = new InternshipApplication(Guid.NewGuid(), candidate, this);
        
        _applications.Add(application);
        candidate.TrackApplication(application);

        return application;
    }

    // Capacity Limit
    public void OfferPosition(InternshipApplication application)
    {
        if (!_applications.Contains(application))
            throw new ApplicationMismatchException("This application does not belong to the current internship.");

        int reservedSpots = _applications.Count(a => 
            a.Status == ApplicationStatus.Enrolled || 
            a.Status == ApplicationStatus.Accepted);
        
        if (reservedSpots >= Capacity)
            throw new CapacityExceededException($"Internship capacity of {Capacity} has been reached.");

        application.MarkAsAccepted();
    }
}