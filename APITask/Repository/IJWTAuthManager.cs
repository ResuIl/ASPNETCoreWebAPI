using Azure;
using System.Security.Claims;

namespace APITask.Repository;

	public interface IJWTAuthManager
	{
		string GenerateSecurityToken(string id, string email, IEnumerable<string> roles, IEnumerable<Claim> userClaims);
	}
