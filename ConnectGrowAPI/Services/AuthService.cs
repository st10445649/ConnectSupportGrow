using ConnectGrowAPI.Data;
using ConnectGrowAPI.Dtos;
using ConnectGrowAPI.Models;
using ConnectGrowAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectGrowAPI.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenService _tokens;
    private readonly ApplicationDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ITokenService tokens,
        ApplicationDbContext db,
        Microsoft.Extensions.Options.IOptions<JwtOptions> jwt,
        ILogger<AuthService> logger)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
        _db = db;
        _jwt = jwt.Value;
        _logger = logger;
    }

    
    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request, string? ip, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.FindByEmailAsync(email) is not null)
        {
            // Registration will show if an email is taken
            return Result<AuthResponse>.Conflict(
                "An account with this email address already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            Organisation = request.Organisation?.Trim(),
            EmailConfirmed = true,   // sets to false once sendgird email authenticaiton vonfirms its live
            IsActive = true
        };

        var created = await _users.CreateAsync(user, request.Password);

        if (!created.Succeeded)
        {
            var errors = string.Join(" ", created.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Invalid(errors);
        }

        await _users.AddToRoleAsync(user, RoleNames.User);

        _logger.LogInformation("New account registered: {UserId}", user.Id);

        return await IssueTokensAsync(user, ip, ct);
    }


    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request, string? ip, CancellationToken ct = default)
    {
        // One message for every failure mode below, so a caller cannot use the
        // response to work out which email addresses have accounts.
        const string genericFailure = "Invalid email address or password.";

        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());

        if (user is null)
        {
            _logger.LogInformation("Login attempt for unknown email from {Ip}.", ip);
            return Result<AuthResponse>.Failure(ErrorType.Forbidden, genericFailure);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt on deactivated account {UserId}.", user.Id);
            return Result<AuthResponse>.Failure(ErrorType.Forbidden, genericFailure);
        }

        // rate-limiting. lockoutOnFailure enables the 5-attempt / 15-minute lockout configured
     
        var signIn = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signIn.IsLockedOut)
        {
            _logger.LogWarning("Account {UserId} is locked out.", user.Id);
            return Result<AuthResponse>.Failure(
                ErrorType.Forbidden,
                "This account is temporarily locked after too many failed attempts. Please try again in 15 minutes.");
        }

        if (!signIn.Succeeded)
        {
            _logger.LogInformation("Failed password attempt for {UserId} from {Ip}.", user.Id, ip);
            return Result<AuthResponse>.Failure(ErrorType.Forbidden, genericFailure);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return await IssueTokensAsync(user, ip, ct);
    }
    // Refresh — with rotation and reuse detection

    public async Task<Result<AuthResponse>> RefreshAsync(
        string rawRefreshToken, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return Result<AuthResponse>.Failure(ErrorType.Forbidden, "No refresh token supplied.");

        var hash = _tokens.HashToken(rawRefreshToken);
        var now = DateTime.UtcNow;

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            return Result<AuthResponse>.Failure(ErrorType.Forbidden, "Invalid refresh token.");

        // Reuse detection. A token that has already been rotated should never be
        // presented again — if it is, either it was stolen or the legitimate
        // client is replaying. Either way the safe response is to end every
        // session for that user and force a fresh login.
        if (stored.RevokedAt is not null)
        {
            _logger.LogWarning(
                "Reuse of a revoked refresh token for user {UserId} from {Ip}. Revoking all sessions.",
                stored.UserId, ip);

            await RevokeAllForUserAsync(stored.UserId, ct);

            return Result<AuthResponse>.Failure(
                ErrorType.Forbidden,
                "Your session is no longer valid. Please sign in again.");
        }

        if (stored.IsExpired(now))
            return Result<AuthResponse>.Failure(ErrorType.Forbidden, "Your session has expired. Please sign in again.");

        if (!stored.User.IsActive)
            return Result<AuthResponse>.Failure(ErrorType.Forbidden, "This account is no longer active.");

        var issued = await IssueTokensAsync(stored.User, ip, ct);
        if (issued.IsFailure) return issued;

      
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = _tokens.HashToken(issued.Value!.RefreshToken);
        await _db.SaveChangesAsync(ct);

        return issued;
    }

    public async Task<Result> LogoutAsync(
        string? rawRefreshToken, Guid userId, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            var hash = _tokens.HashToken(rawRefreshToken);

            var stored = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, ct);

            if (stored is not null && stored.RevokedAt is null)
            {
                stored.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }

        _logger.LogInformation("User {UserId} signed out.", userId);
        return Result.Success();
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return Result<UserProfileDto>.NotFound("User not found.");

        var roles = await _users.GetRolesAsync(user);
        return Result<UserProfileDto>.Success(MapProfile(user, roles));
    }

    public async Task<Result<UserProfileDto>> UpdateProfileAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return Result<UserProfileDto>.NotFound("User not found.");

        var newEmail = request.Email.Trim().ToLowerInvariant();

        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await _users.FindByEmailAsync(newEmail);
            if (taken is not null && taken.Id != userId)
                return Result<UserProfileDto>.Conflict("That email address is already in use.");

            
            var emailResult = await _users.SetEmailAsync(user, newEmail);
            if (!emailResult.Succeeded)
                return Result<UserProfileDto>.Invalid(
                    string.Join(" ", emailResult.Errors.Select(e => e.Description)));

            await _users.SetUserNameAsync(user, newEmail);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.Organisation = request.Organisation?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
            return Result<UserProfileDto>.Invalid(
                string.Join(" ", updated.Errors.Select(e => e.Description)));

        var roles = await _users.GetRolesAsync(user);
        return Result<UserProfileDto>.Success(MapProfile(user, roles));
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound("User not found.");

        var result = await _users.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
            return Result.Invalid(string.Join(" ", result.Errors.Select(e => e.Description)));

        
        await RevokeAllForUserAsync(userId, ct);

        _logger.LogInformation("Password changed for user {UserId}; all sessions revoked.", userId);
        return Result.Success();
    }


    public async Task<Result<string?>> CreatePasswordResetTokenAsync(
        string email, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(email.Trim().ToLowerInvariant());

        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Password reset requested for an unknown or inactive address.");
            return Result<string?>.Success(null);
        }

        var token = await _users.GeneratePasswordResetTokenAsync(user);

        _logger.LogInformation("Password reset token generated for user {UserId}.", user.Id);

        //will be ammended whend doing email integration
        return Result<string?>.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());

        const string genericFailure = "This password reset link is invalid or has expired.";

        if (user is null) return Result.Invalid(genericFailure);

        var result = await _users.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Password reset failed for user {UserId}.", user.Id);
            return Result.Invalid(genericFailure);
        }

        await RevokeAllForUserAsync(user.Id, ct);

        await _users.SetLockoutEndDateAsync(user, null);
        await _users.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("Password reset completed for user {UserId}.", user.Id);
        return Result.Success();
    }

    private async Task<Result<AuthResponse>> IssueTokensAsync(
        ApplicationUser user, string? ip, CancellationToken ct)
    {
        var roles = await _users.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user, roles);
        var (rawRefresh, refreshHash) = _tokens.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedByIp = ip
        });

        await PruneExpiredTokensAsync(user.Id, ct);
        await _db.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefresh,
            AccessTokenExpiresAt = expiresAt,
            User = MapProfile(user, roles)
        });
    }

    private async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in active)
            token.RevokedAt = now;

        await _db.SaveChangesAsync(ct);
    }

    private async Task CutExpiredTokensAsync(Guid userId, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-14);

        var stale = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAt < cutoff)
            .ToListAsync(ct);

        if (stale.Count > 0)
            _db.RefreshTokens.RemoveRange(stale);
    }

    private static UserProfileDto MapProfile(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        Organisation = user.Organisation,
        Roles = roles.ToList(),
        CreatedAt = user.CreatedAt
    };
}