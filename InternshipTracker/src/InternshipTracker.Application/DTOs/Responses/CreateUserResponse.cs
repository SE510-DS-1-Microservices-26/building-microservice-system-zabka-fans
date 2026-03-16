using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs.Responses;

public record CreateUserResponse(Guid Id, string Name, CandidateLevel Level);