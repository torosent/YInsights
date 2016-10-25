using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Azure;
using System.Data.SqlClient;
using Microsoft.Azure.Documents.Client;
using YInsights.Shared;
using System.Text;
using YInsights.Shared.Poco;
using YInsights.Shared.AI;
using YInsights.Shared.Providers;
using YInsights.Shared.Extentions;

namespace UserArticlesGenerator
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class UserArticlesGenerator : StatefulService
    {

        string SqlConnection = CloudConfigurationManager.GetSetting("SqlConnection");
        private DocumentDBProvider documentDBProvider;


        public UserArticlesGenerator(StatefulServiceContext context, DocumentDBProvider documentDBProvider) : base(context)
        {
            this.documentDBProvider = documentDBProvider;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            Task.Run(() =>
            {
                GenerateUsersArticles();
            }, cancellationToken);

            while (true)
            {

                GetUsersAndTopics();
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }


        private async void GetUsersAndTopics()
        {
            using (var sqlConnection =
                     new SqlConnection(SqlConnection))
            {
                sqlConnection.Open();
                var sql = $"SELECT Id,topics FROM [User] where topics is not null";
                var cmd = new SqlCommand(sql, sqlConnection);
                var reader = await cmd.ExecuteReaderAsync();

                var usersDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<string, string>>>("usersDictionary");
                var usersQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<string>>("usersQueue");

                using (var tx = this.StateManager.CreateTransaction())
                {
                    while (reader.Read())
                    {
                        var id = reader[0].ToString();
                        var topics = reader[1].ToString().ToLower();
                        var contains = await usersDictionary.ContainsKeyAsync(tx, id);
                        if (!contains)
                        {
                            await usersDictionary.TryAddAsync(tx, id, new Tuple<string, string>(id, topics));
                            await usersQueue.EnqueueAsync(tx, id);
                        }
                    }
                    await tx.CommitAsync();
                }
            }
        }

        private async void GenerateUsersArticles()
        {
            try
            {
                var usersDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<string, string>>>("usersDictionary");
                var usersQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<string>>("usersQueue");

                while (true)
                {

                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        var idValue = await usersQueue.TryDequeueAsync(tx);
                        if (idValue.HasValue)
                        {
                            var user = await usersDictionary.TryGetValueAsync(tx, idValue.Value);
                            if (user.HasValue)
                            {
                                var id = user.Value.Item1;
                                var topics = (List<string>)Newtonsoft.Json.JsonConvert.DeserializeObject(user.Value.Item2, typeof(List<string>));
                                SearchForArticles(id, topics);
                                await usersDictionary.TryRemoveAsync(tx, user.Value.Item1);
                            }
                            await tx.CommitAsync();
                        }

                    }

                }
            }

            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this, ex.Message, ex);
                ApplicationInsightsClient.LogException(ex);

            }
        }

        private async void SearchForArticles(string id, List<string> topics)
        {
            var existingArticles = await GetUserExistingArticles(id);

            var size = existingArticles.Count % 500;

            var parExistingArticles = existingArticles.Partition(size);


            foreach (var listOfExistingArticles in parExistingArticles)
            {
                int topicIdx = 1;
                var queryBuilder = new StringBuilder("SELECT * FROM articles c WHERE (");

                var paramCollection = new Microsoft.Azure.Documents.SqlParameterCollection();
                foreach (var topic in topics)
                {

                    queryBuilder.Append($" ARRAY_CONTAINS(c.lctags,@topic{topicIdx}) ");
                    if (topicIdx < topics.Count)
                    {
                        queryBuilder.Append("OR ");
                    }
                    else
                    {
                        queryBuilder.Append(")");
                    }
                    paramCollection.Add(new Microsoft.Azure.Documents.SqlParameter { Name = $"@topic{topicIdx}", Value = topic });

                    topicIdx++;
                }

                if (listOfExistingArticles.Count > 0)
                {
                    queryBuilder.Append($" AND c.id NOT IN ( ");
                    int idIdx = 1;
                    foreach (var item in listOfExistingArticles)
                    {
                        queryBuilder.Append('"');
                        queryBuilder.Append(item);
                        queryBuilder.Append('"');
                        if (idIdx < listOfExistingArticles.Count)
                        {
                            queryBuilder.Append(",");
                        }

                        idIdx++;
                    }
                    queryBuilder.Append(')');
                }

                var queryString = queryBuilder.ToString();
                var query = new Microsoft.Azure.Documents.SqlQuerySpec(queryString, paramCollection);

                var articleQuery = documentDBProvider.Client.CreateDocumentQuery<Article>(
                    UriFactory.CreateDocumentCollectionUri("articles", "article"), query).AsEnumerable().Select(x => x.Id);
                InsertNewArticles(id, articleQuery.ToList());
            }




        }
        private async void InsertNewArticles(string username, List<string> articles)
        {
            if (articles.Count > 0)
            {
                using (var sqlConnection =
                           new SqlConnection(SqlConnection))
                {
                    sqlConnection.Open();
                    var trans = sqlConnection.BeginTransaction();

                    foreach (var article in articles)
                    {
                        var insertCmd = new SqlCommand("INSERT INTO UserArticles(username,articleid,isviewed,addeddate) VALUES(@param1,@param2,@param3,@param4)", sqlConnection);
                        insertCmd.Parameters.Add(new SqlParameter("@param1", username));
                        insertCmd.Parameters.Add(new SqlParameter("@param2", article));
                        insertCmd.Parameters.Add(new SqlParameter("@param3", false));
                        insertCmd.Parameters.Add(new SqlParameter("@param4", DateTime.Now));
                        insertCmd.Transaction = trans;
                        await insertCmd.ExecuteNonQueryAsync();
                        ApplicationInsightsClient.LogEvent("Added article for user", username, article);

                    }

                    trans.Commit();

                }
            }
        }

        private async Task<List<string>> GetUserExistingArticles(string id)
        {
            var articles = new List<string>();
            using (var sqlConnection =
                    new SqlConnection(SqlConnection))
            {
                sqlConnection.Open();
                var sql = $"SELECT articleid FROM UserArticles where username = '{id}'";
                var cmd = new SqlCommand(sql, sqlConnection);
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    var articleid = reader[0].ToString();
                    articles.Add(articleid);
                }
            }
            return articles;
        }
    }
}
