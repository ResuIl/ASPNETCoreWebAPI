namespace APITask.Models.DTOs;

public class RegisterRequest
{
	public string Username { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public IFormFile ProfilePhoto { get; set; }
}
