using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Improbability.Models
{
    /// <summary>
    /// The event of a RandomItem that results in a number
    /// </summary>
    public class RandomEvent
    {
        /// <summary>
        /// The id you need to for specific requests
        /// </summary>
        /// <example>26</example>
        [Ignore] public int Id { get; set; }

        /// <summary>
        /// Optional, the name you want
        /// </summary>
        /// <example>First roll</example>
        [Index(0)] public string Name { get; set; }

        /// <summary>
        /// The time at wich the RandomEvent occur
        /// </summary>
        /// <example>2009-01-01T12:48:35+01:00</example>
        [Index(1)] [Required] public string Time { get; set; }

        /// <summary>
        /// The number created by the event
        /// </summary>
        /// <example>8</example>
        [Index(2)] [BindRequired] public int Result { get; set; }

        /// <summary>
        /// An optional field to say more about your RandomEvent
        /// </summary>
        /// <example>And they all lived happily everafter</example>
        [Index(3)] public string Description { get; set; }

        /// <summary>
        /// The id of the RandomItem that the event is associated with.
        /// </summary>
        /// <example>1</example>
        [Index(4)] [BindRequired] public int RandomItemId { get; set; }
    }
}
