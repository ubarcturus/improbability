﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

namespace Improbability.Models
{
    public class RandomItem
    {
        [SwaggerSchema(ReadOnly = true)]
        public int Id { get; set; }

        [Required] public string Name { get; set; }
        [Required] public int NumberOfPossibleResults { get; set; }
        public string Description { get; set; }
    }
}
