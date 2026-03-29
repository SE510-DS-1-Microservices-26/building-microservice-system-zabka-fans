namespace CoreService.Application.DTOs;

public record PaginatedRequest(int Page = 1, int PageSize = 10);

