using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Improbability.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required] public string ApiKey { get; set; }

        public Collection<RandomItem> RandomItems { get; }
    }
}
