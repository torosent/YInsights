using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Model
{
    public class UserArticles
    {
        [JsonIgnore]
        [Key]
        public int Id { get; set; }
        [JsonIgnore]
        public string username { get; set; }
        public int articleid { get; set; }
        public DateTime? addeddate { get; set; }

        public bool isviewed { get; set; }

        [NotMapped]
        public List<string> tags { get; set; }
        [NotMapped]
        public string url { get; set; }
        [NotMapped]
        public string title { get; set; }

        [NotMapped]
        public int time { get; set; }
        [NotMapped]
        public int id { get; set; }
    }
}
