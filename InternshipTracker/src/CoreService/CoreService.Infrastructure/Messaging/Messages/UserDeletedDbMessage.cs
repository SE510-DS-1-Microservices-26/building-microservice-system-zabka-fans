using System.ComponentModel.DataAnnotations;

namespace CoreService.Infrastructure.Messaging.Messages;

public class UserDeletedDbMessage
{
    [Required] public Guid UserId { get; set; }
}