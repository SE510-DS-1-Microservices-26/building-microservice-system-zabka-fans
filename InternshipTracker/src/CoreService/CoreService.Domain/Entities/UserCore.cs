using CoreService.Domain.Enums;

namespace CoreService.Domain.Entities;

public class UserCore
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public CandidateLevel Level { get; private set; }
    
    public UserCore(Guid id, string name, CandidateLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        
        Id = id;
        Name = name;
        Level = level;
    }

    private UserCore() { }
}