using MongoDB.Bson;
using MongoDB.Driver;
using RevoltImageGenApi.Controllers;

namespace RevoltImageGenApi
{
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMongoCollection<BsonDocument> _collection = Utils.database.GetCollection<BsonDocument>(ConfigAndDefaults.mongoEndpointTelemetryCollection);
        private readonly IMongoCollection<BsonDocument> _logsCollection = Utils.getCollection<BsonDocument>(ConfigAndDefaults.mongoEndpointLogCollection);

        public TelemetryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString();

            if (!path.StartsWith("/api/metrics") && !path.StartsWith("/swagger"))
            {
                var endpointUri = $"{context.Request.Method} {path}";
                var requestTime = DateTime.UtcNow;

                // Run request
                await _next(context);

                var statusCode = context.Response.StatusCode;
                bool success = statusCode < 400;

                var filter = Builders<BsonDocument>.Filter.Eq("endpoint", endpointUri);
                var update = Builders<BsonDocument>.Update.Inc("count", 1);
                await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });

                var logDoc = new BsonDocument
                {
                    { "endpoint", endpointUri },
                    { "timestamp", requestTime },
                    { "statusCode", statusCode },
                    { "success", success }
                };
                await _logsCollection.InsertOneAsync(logDoc);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
