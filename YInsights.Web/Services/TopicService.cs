using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YInsights.Web.Model;
using YInsights.Web.Providers;
using YInsights.Web.Extentions;
namespace YInsights.Web.Services
{
    public class TopicService
    {
        private readonly YInsightsContext db;    
        private readonly RedisService redisdb;

        public TopicService(YInsightsContext _db, RedisService _redisdb)
        {
            db = _db;
            redisdb = _redisdb;
        }
        public async Task<IEnumerable<Topics>> GetTopics(int limit)
        {
            var strTopics = await redisdb.Database.StringGetAsync("WordCloudTopics");
            if (!string.IsNullOrEmpty(strTopics))
            {
                var topics = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Topics>>(strTopics).Take(limit);
                return topics;
            }
            return new List<Topics>();
        }

        public async Task<IEnumerable<string>> SearchTopics(string text, int limit)
        {
            var strTopics = await redisdb.Database.StringGetAsync("Topics");
            if (!string.IsNullOrEmpty(strTopics))
            {
                var topics = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Topics>>(strTopics);
                var stringTopics = topics.Where(x => x.topic.Contains(text, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.count).Take(limit).Select(x => x.topic);
                  return stringTopics;
            }
            return new List<string>();

        }
    }
}
