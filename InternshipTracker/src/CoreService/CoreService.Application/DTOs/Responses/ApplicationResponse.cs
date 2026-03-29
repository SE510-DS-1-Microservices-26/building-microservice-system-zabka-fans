using CoreService.Domain.Enums;

namespace CoreService.Application.DTOs.Responses;

public record ApplicationResponse(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    CandidateLevel CandidateLevel,
    string InternshipTitle,
    ApplicationStatus Status);

