using System.Text.RegularExpressions;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Exceptions;

namespace UserService.Domain.Factories;

public static class UserFactory
{
    // RFC 5321 practical limit: local@domain, max 320 chars total
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    public static User Create(Guid id, string name, string email, CandidateLevel level)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidEmailException(email ?? string.Empty);

        if (email.Length > 320 || !EmailRegex.IsMatch(email))
            throw new InvalidEmailException(email);

        return new User(id, name, email, level);
    }
}
