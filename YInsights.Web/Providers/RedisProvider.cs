using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Providers
{
    public class RedisProvider:IRedisProvider
    {
        ConnectionMultiplexer redis;
        private IDatabase Database { get; set; }
        public RedisProvider(string connectionString)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
            Database = redis.GetDatabase();
            
        }

        public async void SetValue(string key, string value)
        {
            await Database.StringSetAsync(key, value);
           
        }

        public async Task<string> GetValue(string key)
        {
            var val = await Database.StringGetAsync(key);
            return (val.HasValue==true) ? val.ToString() : null;
        }
    }
}
