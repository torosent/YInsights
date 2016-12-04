using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode.Poco
{
    public class Article
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public int score { get; set; }
        public int time { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public bool processed { get; set; }

        public List<string> topics { get; set; }

        public List<string> tags { get; set; }
        public List<string> lctags { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
