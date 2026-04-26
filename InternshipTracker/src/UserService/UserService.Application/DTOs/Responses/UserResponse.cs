using UserService.Domain.Enums;

namespace UserService.Application.DTOs.Responses;

public record UserResponse(Guid Id, string Name, string Email, CandidateLevel Level);
