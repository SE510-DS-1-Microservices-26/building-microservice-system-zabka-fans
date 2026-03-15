using System.Net.Mime;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;

namespace InternshipTracker.Domain.Entities;

public class Internship : IEntity
{
    public Guid Id { get; set; }
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
        {
            throw new DomainException(
                $"Candidate level '{candidate.Level}' does not meet the minimum requirement of '{MinimumLevel}'.");
        }

        var application = new InternshipApplication(Guid.NewGuid(), candidate, this);

        _applications.Add(application);
        candidate.TrackApplication(application);

        return application;
    }

    // Capacity Limit
    public void OfferPosition(InternshipApplication internshipApplication)
    {
        if (!_applications.Contains(internshipApplication))
            throw new InvalidOperationException("Application does not belong to this internship.");

        // We count both Enrolled and Accepted. If you have 5 spots and offer 5, 
        // you shouldn't offer a 6th just because the first 5 haven't clicked "Enroll" yet.
        int reservedSpots = _applications.Count(a =>
            a.Status == ApplicationStatus.Enrolled ||
            a.Status == ApplicationStatus.Accepted);

        if (reservedSpots >= Capacity)
        {
            throw new DomainException("Internship capacity has been reached. Cannot accept more applicants.");
        }

        internshipApplication.MarkAsAccepted();
    }
}