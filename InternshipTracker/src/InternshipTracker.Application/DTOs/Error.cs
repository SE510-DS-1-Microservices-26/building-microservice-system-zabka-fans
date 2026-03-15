using InternshipTracker.Application.Enums;

namespace InternshipTracker.Application.DTOs;

public record Error(string Code, string Description, ErrorType Type);