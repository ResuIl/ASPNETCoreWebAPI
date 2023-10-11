using Microsoft.AspNetCore.Identity;

namespace APITask.Models;

public class ApplicationUser : IdentityUser
{
	public string? RefreshToken { get; set; }
	public string ProfilePhotoUrl { get; set; }
}
