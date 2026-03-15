using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs;

public record CreateInternshipRequest(string Title, int Capacity, CandidateLevel MinimumLevel);
