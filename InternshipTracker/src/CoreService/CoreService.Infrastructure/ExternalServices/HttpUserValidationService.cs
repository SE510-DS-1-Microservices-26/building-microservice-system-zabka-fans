using System.Net;
using System.Net.Http.Json;
using CoreService.Application.Interfaces;

namespace CoreService.Infrastructure.ExternalServices;

public class HttpUserValidationService : IUserValidationService
{
    private readonly HttpClient _httpClient;
    
    public HttpUserValidationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<UserInfo?> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken)  // used by use case
    {
        var response = await _httpClient.GetAsync($"/users/{userId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
    }
}