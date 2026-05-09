using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notely.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        [Required]
        [MaxLength(30)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        public DateTime Created_at { get; set; } = DateTime.Now;

        [MaxLength(300)]
        public string? ProfileImagepath { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "User";

        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}
