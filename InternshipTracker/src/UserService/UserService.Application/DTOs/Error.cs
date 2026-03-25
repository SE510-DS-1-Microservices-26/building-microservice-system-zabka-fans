using UserService.Application.Enums;

namespace UserService.Application.DTOs;

public record Error(string Code, string Description, ErrorType Type);