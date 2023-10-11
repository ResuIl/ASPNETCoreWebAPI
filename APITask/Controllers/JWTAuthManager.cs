using APITask.Repository;
using Azure;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APITask.Controllers;

public class JWTAuthManager : IJWTAuthManager
{
	private readonly IConfiguration _configuration;
	public JWTAuthManager(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string GenerateSecurityToken(string id, string email, IEnumerable<string> roles, IEnumerable<Claim> userClaims)
	{
		var claims = new[]
		{
				new Claim(ClaimsIdentity.DefaultNameClaimType, email),
				new Claim("userId", id),
				new Claim(ClaimsIdentity.DefaultRoleClaimType, string.Join(",", roles))
			}.Concat(userClaims);

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));

		var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _configuration["JWT:Issuer"],
			audience: _configuration["JWT:Audience"],
			expires: DateTime.UtcNow.AddMinutes(3), // ExpiresInMinutes
			signingCredentials: signingCredentials,
			claims: claims
			);

		var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

		return accessToken;
	}
}
