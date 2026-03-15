using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.Enums;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Entities;

namespace InternshipTracker.Application.UseCases;

public class CreateUserUseCase : IUseCase<CreateUserRequest, UserResponse>
{
    private readonly IUserRepository _userRepository;

    public CreateUserUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponse>> ExecuteAsync(
        CreateUserRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = new User(Guid.NewGuid(), request.Name, request.Level);
            
            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            var response = new UserResponse(user.Id, user.Name, user.Level);
            return Result<UserResponse>.Success(response);
        }
        catch (Exception)
        {
            return Result<UserResponse>.Failure(new Error(
                "System.Failure", 
                "An unexpected error occurred while creating the user.", 
                ErrorType.Failure));
        }
    }
}