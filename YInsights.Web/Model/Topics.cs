using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Model
{
    public class Topics
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        public string topic { get; set; }
        public int count { get; set; }


    }
}
