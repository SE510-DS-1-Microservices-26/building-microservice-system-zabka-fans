using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs.Responses;

public record ApplyForInternshipResponse(Guid ApplicationId, ApplicationStatus Status);