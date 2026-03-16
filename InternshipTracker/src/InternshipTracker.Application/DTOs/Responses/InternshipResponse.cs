using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs.Responses;

public record InternshipResponse(Guid Id, string Title, int Capacity, CandidateLevel MinimumLevel);

