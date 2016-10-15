using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YInsights.Shared.Poco;

namespace YInsights.Shared.Common
{
    public static class Tags
    {
        public static void CalculateTags(Dictionary<string, int> tags, Article article, bool includeTags = true)
        {
            if (includeTags)
                foreach (var tag in article.tags)
                {
                    if (tag.Contains("_"))
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

            foreach (var topic in article.topics)
            {
                if (topic.Contains("_"))
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
