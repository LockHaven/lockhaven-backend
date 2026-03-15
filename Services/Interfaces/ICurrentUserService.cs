namespace lockhaven_backend.Services.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
}