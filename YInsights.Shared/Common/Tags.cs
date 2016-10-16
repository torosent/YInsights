using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YInsights.Shared.Extentions;
using YInsights.Shared.Poco;

namespace YInsights.Shared.Common
{
    public static class Tags
    {
        private static string[] Exclude = { "Computing", "Software", "Business", "Economy", "Human Interest", "Politics", "Environment", "Social Issues","HN","Labor","Other" };
        public static void CalculateTags(Dictionary<string, int> tags, Article article, bool includeTags = true)
        {

            foreach (var tag in article.tags)
            {
                if (tag.Contains("_") || Exclude.Contains(tag, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (tags.ContainsKey(tag))
                {
                    tags[tag]++;
                   
                }
                else
                {
                    tags.Add(tag, 1);
                }
               
            }
            if (includeTags)
                foreach (var topic in article.topics)
                {
                    if (topic.Contains("_") || Exclude.Contains(topic, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (tags.ContainsKey(topic))
                    {
                        tags[topic]++;
                    }
                    else
                    {
                        tags.Add(topic, 1);
                    }
                }
        }
    }

}
