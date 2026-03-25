using CoreService.Domain.Enums;

namespace CoreService.Application.DTOs.Responses;

public record ApplyForInternshipResponse(Guid ApplicationId, ApplicationStatus Status);