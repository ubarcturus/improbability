using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

namespace Improbability.Models
{
    public class RandomEvent
    {
        [SwaggerSchema(ReadOnly = true)]
        public int Id { get; set; }

        public string Name { get; set; }
        [Required] public string Time { get; set; }
        [Required] public int Result { get; set; }
        public string Description { get; set; }
        [Required] public int RandomItemId { get; set; }
    }
}
