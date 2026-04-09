using UserService.Domain.Enums;

namespace UserService.Application.DTOs.Requests;

public record CreateUserRequest(string Name, string Email, CandidateLevel Level);
