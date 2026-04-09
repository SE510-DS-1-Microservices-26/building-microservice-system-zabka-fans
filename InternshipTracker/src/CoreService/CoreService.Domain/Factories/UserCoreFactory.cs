using System.Text.RegularExpressions;
using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Interfaces;

namespace CoreService.Domain.Factories;

public sealed class UserCoreFactory : IUserCoreFactory
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    public UserCore Create(Guid id, string name, string email, CandidateLevel level)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidEmailException(email ?? string.Empty);

        if (email.Length > 320 || !EmailRegex.IsMatch(email))
            throw new InvalidEmailException(email);

        return new UserCore(id, name, email, level);
    }
}

