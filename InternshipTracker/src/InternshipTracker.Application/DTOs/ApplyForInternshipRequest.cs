namespace InternshipTracker.Application.DTOs;

public record ApplyForInternshipRequest(Guid UserId, Guid InternshipId);