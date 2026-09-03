using System.ComponentModel.DataAnnotations;

namespace ConnectGrow.Models;


public class LoginInputModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
 
public class RegisterInputModel
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
 
    [Required(ErrorMessage = "Choose a password.")]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
 
    [Required(ErrorMessage = "Confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
 
    [Display(Name = "Practice or organisation")]
    public string? Organisation { get; set; }
 
    public string? ReturnUrl { get; set; }
}
