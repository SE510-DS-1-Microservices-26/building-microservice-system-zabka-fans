using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class User
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? CorporateEmail { get; private set; }
    public CandidateLevel Level { get; private set; }

    public User(Guid id, string name, string email, CandidateLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        Id = id;
        Name = name;
        Email = email;
        Level = level;
    }

    public void SetCorporateEmail(string corporateEmail)
    {
        if (string.IsNullOrWhiteSpace(corporateEmail))
            throw new ArgumentException("Corporate email cannot be empty.", nameof(corporateEmail));

        CorporateEmail = corporateEmail;
    }

    // For EF core
    private User()
    {
    }
}