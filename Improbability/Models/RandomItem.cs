using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;
using Swashbuckle.AspNetCore.Annotations;

namespace Improbability.Models
{
    public class RandomItem
    {
        [SwaggerSchema(ReadOnly = true)]
        [Ignore]
        public int Id { get; set; }

        [Index(0)] [Required] public string Name { get; set; }
        [Index(1)] [Required] public int NumberOfPossibleResults { get; set; }
        [Index(2)] public string Description { get; set; }
    }
}
