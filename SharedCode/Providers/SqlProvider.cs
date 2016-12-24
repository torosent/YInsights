using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharedCode.Providers
{
    public class SqlProvider:IDatabaseProvider
    {
        public string ConnectionString { get; set; }

    }
}
