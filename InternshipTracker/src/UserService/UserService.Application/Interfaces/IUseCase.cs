using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

public interface IUseCase<in TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface IUseCase<in TRequest>
{
    Task<Result> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}