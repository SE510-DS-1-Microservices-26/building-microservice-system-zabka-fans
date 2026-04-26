using CoreService.Application.Enums;

namespace CoreService.Application.DTOs;

public record Error(string Code, string Description, ErrorType Type);