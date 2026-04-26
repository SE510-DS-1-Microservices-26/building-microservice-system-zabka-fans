namespace CoreService.Application.DTOs.Requests;

public record ApplyForInternshipRequest(Guid UserId, Guid InternshipId);