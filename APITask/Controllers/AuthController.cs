using APITask.Models;
using APITask.Models.DTOs;
using APITask.Repository;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace APITask.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJWTAuthManager _jwtService;
	private readonly IConfiguration _configuration;

	public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager , IConfiguration configuration, IJWTAuthManager jwtService)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_configuration = configuration;
		_jwtService = jwtService;
	}

	[AllowAnonymous]
	[HttpPost("register")]
	public async Task<IActionResult> Register([FromForm] RegisterRequest model)
	{
		string profilePhotoUrl = "defaultURL";
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		if (model.ProfilePhoto != null)
		{
			profilePhotoUrl = await UploadProfilePhotoToBlobStorage(model.ProfilePhoto);
		}

		var user = new ApplicationUser
		{
			UserName = model.Username,
			Email = model.Email,
			ProfilePhotoUrl = profilePhotoUrl
		};

		var result = await _userManager.CreateAsync(user, model.Password);

		if (result.Succeeded)
		{
			return Ok("Registration successful.");
		}
		else
		{
			return BadRequest(result.Errors);
		}
	}

	[AllowAnonymous]
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest model)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var user = await _userManager.FindByNameAsync(model.Username);

		if (user == null)
		{
			return Unauthorized("Invalid username or password.");
		}

        var canSignIn = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!canSignIn.Succeeded)
            return BadRequest();

        var token = GenerateToken(user);

		return Ok(new { Token = token });
	}

	[HttpPut("edit-profile")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> EditProfile([FromForm] ProfileDto model)
	{
        var user = HttpContext.User;

		if (user == null)
		{
			return Unauthorized("User not found.");
		}

        var currentUser = await _userManager.FindByEmailAsync(user.Identity.Name);

        if (!string.IsNullOrWhiteSpace(model.Username))
		{
			currentUser.UserName = model.Username;
		}

		if (!string.IsNullOrWhiteSpace(model.Email))
		{
			currentUser.Email = model.Email;
		}

		if (model.NewProfilePhoto != null)
		{
			string newProfilePhotoUrl = await UploadProfilePhotoToBlobStorage(model.NewProfilePhoto);
			currentUser.ProfilePhotoUrl = newProfilePhotoUrl;
		}

		var result = await _userManager.UpdateAsync(currentUser);

		if (result.Succeeded)
		{
			return Ok("Profile updated successfully.");
		}
		else
		{
			return BadRequest(result.Errors);
		}
	}


	[HttpGet("getProfilePicture")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetProfilePicture()
	{
		var currentUser = HttpContext.User;

        if (currentUser == null)
        {
            return Unauthorized();
        }
		var user = await _userManager.FindByEmailAsync(currentUser.Identity.Name);

        return Ok(new { ProfilePicture = user.ProfilePhotoUrl });
	}

	private async Task<AuthTokenDTO> GenerateToken(ApplicationUser user)
	{
		var roles = await _userManager.GetRolesAsync(user);
		var claims = await _userManager.GetClaimsAsync(user);

		var accessToken = _jwtService.GenerateSecurityToken(user.Id, user.Email, roles, claims);

		var refreshToken = Guid.NewGuid().ToString("N").ToLower();

		user.RefreshToken = refreshToken;
		await _userManager.UpdateAsync(user);

		return new AuthTokenDTO
		{
			AccessToken = accessToken,
			RefreshToken = refreshToken,
		};
	}

	private async Task<string> UploadProfilePhotoToBlobStorage(IFormFile profilePhoto)
	{
		// Local Testing

		//      if (profilePhoto != null && profilePhoto.Length > 0)
		//      {
		//          // 
		//          var filePath = Path.Combine(Directory.GetCurrentDirectory(), "images", profilePhoto.FileName);
		//          using (var stream = new FileStream(filePath, FileMode.Create))
		//          {
		//              await profilePhoto.CopyToAsync(stream);
		//          }

		//          return "/images/" + profilePhoto.FileName;
		//      }

		//      return null;

		var blobServiceClient = new BlobServiceClient(_configuration["ConnectionStrings:AzureBlobStorage"]);
		var containerClient = blobServiceClient.GetBlobContainerClient("profilepictures");

		string fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePhoto.FileName);
		var blobClient = containerClient.GetBlobClient(fileName);

		using (var stream = profilePhoto.OpenReadStream())
		{
			await blobClient.UploadAsync(stream, true);
		}

		return blobClient.Uri.ToString();
	}
}
