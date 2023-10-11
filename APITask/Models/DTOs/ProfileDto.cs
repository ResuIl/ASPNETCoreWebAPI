namespace APITask.Models.DTOs;

public class ProfileDto
{
	public string Username { get; set; }
	public string Email { get; set; }
	public IFormFile NewProfilePhoto { get; set; }
}
