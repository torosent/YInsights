using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Model
{
    public class User
    {
        [Key]
        public string Id { get; set; }

        public string topics { get; set; }
        public string hn { get; set; }

        public DateTime? lastlogin { get; set; }
        [NotMapped]
        public List<string> tags { get; set; }
        public string username { get; set; }

    }
}
