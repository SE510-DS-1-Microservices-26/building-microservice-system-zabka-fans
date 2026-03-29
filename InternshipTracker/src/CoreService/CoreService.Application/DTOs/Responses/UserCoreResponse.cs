using CoreService.Domain.Enums;

namespace CoreService.Application.DTOs.Responses;

public record UserCoreResponse(Guid Id, string Name, CandidateLevel Level);

