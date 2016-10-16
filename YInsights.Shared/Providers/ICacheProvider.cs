using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace YInsights.Shared.Providers
{
    public interface ICacheProvider
    {
        void SetValue(string key, string value);
        Task<string> GetValue(string key);
    }
}
