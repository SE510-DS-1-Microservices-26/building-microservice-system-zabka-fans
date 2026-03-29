namespace CoreService.Application.DTOs.Requests;

public record GetAllApplicationsRequest(int Page = 1, int PageSize = 10);

