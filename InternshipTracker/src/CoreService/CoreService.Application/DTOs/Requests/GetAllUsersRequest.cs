namespace CoreService.Application.DTOs.Requests;

public record GetAllUsersRequest(int Page = 1, int PageSize = 10);

