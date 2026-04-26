using CoreService.Domain.Enums;

namespace CoreService.Application.DTOs.Responses;

public record InternshipResponse(Guid Id, string Title, int Capacity, CandidateLevel MinimumLevel);