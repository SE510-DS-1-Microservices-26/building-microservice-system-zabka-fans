using UserService.Domain.Enums;

namespace UserService.Domain.Entities;

public class User
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public CandidateLevel Level { get; private set; }

    public User(Guid id, string name, CandidateLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Id = id;
        Name = name;
        Level = level;
    }

    // For EF core
    private User()
    {
    }
}