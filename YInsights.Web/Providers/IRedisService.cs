using StackExchange.Redis;

namespace YInsights.Web.Providers
{
    public interface IRedisService
    {
         IDatabase Database { get; set; }
    }
}