using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Providers
{
    public class RedisService
    {
        ConnectionMultiplexer redis;
        public IDatabase Database { get; set; }
        public RedisService(string connectionString)
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
            Database = redis.GetDatabase();
            
        }

     
    }
}
