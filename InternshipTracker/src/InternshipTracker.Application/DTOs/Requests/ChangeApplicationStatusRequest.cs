using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs.Requests;

public record ChangeApplicationStatusRequest(Guid ApplicationId, ApplicationStatus NewStatus);