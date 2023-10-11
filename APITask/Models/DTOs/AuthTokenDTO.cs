namespace APITask.Models.DTOs;

public class AuthTokenDTO
{
	public string AccessToken { get; set; } = string.Empty;
	public string RefreshToken { get; set; } = string.Empty;
}
