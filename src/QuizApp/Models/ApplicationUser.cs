using Microsoft.AspNetCore.Identity;

namespace QuizApp.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
