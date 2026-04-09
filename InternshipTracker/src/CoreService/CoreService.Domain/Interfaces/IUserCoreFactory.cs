using CoreService.Domain.Entities;
using CoreService.Domain.Enums;

namespace CoreService.Domain.Interfaces;

public interface IUserCoreFactory
{
    UserCore Create(Guid id, string name, string email, CandidateLevel level);
}

