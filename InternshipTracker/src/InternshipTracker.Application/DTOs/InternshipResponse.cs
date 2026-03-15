using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs;

public record InternshipResponse(Guid Id, string Title, int Capacity, CandidateLevel MinimumLevel);

