using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs;

public record UserResponse(Guid Id, string Name, CandidateLevel Level);