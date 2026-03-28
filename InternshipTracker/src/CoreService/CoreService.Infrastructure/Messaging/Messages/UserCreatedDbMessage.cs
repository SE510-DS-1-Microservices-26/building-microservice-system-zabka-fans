using System.ComponentModel.DataAnnotations;
using CoreService.Domain.Enums;

namespace CoreService.Infrastructure.Messaging.Messages;

public class UserCreatedDbMessage
{
    [Required]
    public Guid Id { get; init; }
    [Required]
    public string Name { get; private set; }
    [Required]
    public CandidateLevel Level { get; private set; }
    
    public UserCreatedDbMessage(Guid id, string name, CandidateLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        
        Id = id;
        Name = name;
        Level = level;
    }
}