using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Improbability.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required] public string ApiKey { get; set; }

        public Collection<RandomItem> RandomItems { get; }
    }
}
