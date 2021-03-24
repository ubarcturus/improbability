using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Improbability.Models
{
    /// <summary>
    /// An object or item that generated or stored random numbers
    /// </summary>
    public class RandomItem
    {
        /// <summary>
        /// The id you need to for specific requests
        /// </summary>
        /// <example>1</example>
        [Ignore] public int Id { get; set; }

        /// <summary>
        /// The name you want
        /// </summary>
        /// <example>Blue dice</example>
        [Index(0)] [Required] public string Name { get; set; }

        /// <summary>
        /// How many different Results are possible
        /// </summary>
        /// <example>27</example>
        [Index(1)] [BindRequired] public int NumberOfPossibleResults { get; set; }

        /// <summary>
        /// An optional field to say more about your RandomItem
        /// </summary>
        /// <example>And they all lived happily everafter</example>
        [Index(2)] public string Description { get; set; }
    }
}
