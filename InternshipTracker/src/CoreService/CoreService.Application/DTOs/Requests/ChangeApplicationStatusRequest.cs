using CoreService.Domain.Enums;

namespace CoreService.Application.DTOs.Requests;

public record ChangeApplicationStatusRequest(Guid ApplicationId, ApplicationStatus NewStatus);