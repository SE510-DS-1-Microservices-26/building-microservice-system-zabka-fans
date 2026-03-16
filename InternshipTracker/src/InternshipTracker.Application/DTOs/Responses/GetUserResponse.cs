using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs.Responses;

public record GetUserResponse(Guid Id, string Name, CandidateLevel Level);