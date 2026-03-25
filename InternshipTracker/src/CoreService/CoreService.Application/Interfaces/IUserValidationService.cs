using CoreService.Domain.Enums;

namespace CoreService.Application.Interfaces;

public record UserInfo(Guid UserId, string Name, CandidateLevel Level);

public interface IUserValidationService
{
    Task<UserInfo?> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken);
}