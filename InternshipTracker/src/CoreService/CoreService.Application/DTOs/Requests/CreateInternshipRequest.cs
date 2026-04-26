using CoreService.Domain.Enums;

namespace CoreService.Application.DTOs.Requests;

public record CreateInternshipRequest(string Title, int Capacity, CandidateLevel MinimumLevel);