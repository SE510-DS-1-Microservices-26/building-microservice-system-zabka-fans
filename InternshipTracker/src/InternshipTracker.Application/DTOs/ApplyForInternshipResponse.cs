using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs;

public record ApplyForInternshipResponse(Guid ApplicationId, ApplicationStatus Status);