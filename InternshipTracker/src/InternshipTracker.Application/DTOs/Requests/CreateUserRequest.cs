using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs.Requests;

public record CreateUserRequest(string Name, CandidateLevel Level);