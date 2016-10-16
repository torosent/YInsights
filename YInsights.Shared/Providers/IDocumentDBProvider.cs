using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YInsights.Shared.Providers
{
    public interface IDocumentDBProvider :IDatabaseProvider
    {
        DocumentClient Client { get; set; }

    }
}
