using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.DTOs.Requests;

public record CreateInternshipRequest(string Title, int Capacity, CandidateLevel MinimumLevel);