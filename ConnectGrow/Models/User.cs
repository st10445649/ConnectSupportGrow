using System.ComponentModel.DataAnnotations;

namespace ConnectGrow.Models;
public class Users
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Organisation { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
 
public class AuthResponseModel
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public Users User { get; set; } = null!;
}


public class UpdateProfileInputModel
{
    [Required(ErrorMessage = "Enter your first name.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Enter your last name.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;
 
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }
 
    [Display(Name = "Practice or organisation")]
    public string? Organisation { get; set; }
}
 
public class ChangePasswordInputModel
{
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Choose a new password.")]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
 
public class ForgotPasswordInputModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;
}
 
public class ResetPasswordInputModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Choose a new password.")]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]


    public string ConfirmPassword { get; set; } = string.Empty;
}
public class SettingsViewModel
{
    public UpdateProfileInputModel Profile { get; set; } = new();
    public ChangePasswordInputModel Password { get; set; } = new();
 
    public List<string> Roles { get; set; } = new();
    public DateTime MemberSince { get; set; }
 
    public string ActiveTab { get; set; } = "profile";
}

 public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    public string? DevResetToken { get; set; }
}