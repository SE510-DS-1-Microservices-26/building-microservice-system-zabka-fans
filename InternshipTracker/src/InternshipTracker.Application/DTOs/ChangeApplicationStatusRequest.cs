using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs;

public record ChangeApplicationStatusRequest(Guid ApplicationId, ApplicationStatus NewStatus);