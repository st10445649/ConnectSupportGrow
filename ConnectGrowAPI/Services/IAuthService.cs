using ConnectGrowAPI.Dtos;
using ConnectGrowAPI.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, string? ip, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(string rawRefreshToken, string? ip, CancellationToken ct = default);
    Task<Result> LogoutAsync(string? rawRefreshToken, Guid userId, CancellationToken ct = default);

    Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);


    // Generates a password reset token. Returns the token so the caller can
    // email it; the endpoint never returns it to the client outside development.
    
    Task<Result<string?>> CreatePasswordResetTokenAsync(string email, CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}