using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs;

public record CreateUserRequest(string Name, CandidateLevel Level);