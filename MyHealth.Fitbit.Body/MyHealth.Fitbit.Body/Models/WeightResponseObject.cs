using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MyHealth.Fitbit.Body.Models
{
    [ExcludeFromCodeCoverage]
    public class WeightResponseObject
    {
        public List<Weight> weight { get; set; }
    }
}
