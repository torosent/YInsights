using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode.Providers
{
    public interface IDatabaseProvider
    {
        string ConnectionString { get; set; }
    }
}
