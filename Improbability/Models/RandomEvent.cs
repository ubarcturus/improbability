using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;
using Swashbuckle.AspNetCore.Annotations;

namespace Improbability.Models
{
    public class RandomEvent
    {
        [SwaggerSchema(ReadOnly = true)]
        [Ignore]
        public int Id { get; set; }

        [Index(0)] public string Name { get; set; }
        [Index(1)] [Required] public string Time { get; set; }
        [Index(2)] [Required] public int Result { get; set; }
        [Index(3)] public string Description { get; set; }
        [Index(4)] [Required] public int RandomItemId { get; set; }
    }
}
