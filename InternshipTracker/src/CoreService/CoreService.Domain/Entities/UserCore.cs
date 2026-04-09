using CoreService.Domain.Enums;

namespace CoreService.Domain.Entities;

public class UserCore
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public CandidateLevel Level { get; private set; }
    
    public UserCore(Guid id, string name, string email, CandidateLevel level)
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

    private UserCore() { }
}