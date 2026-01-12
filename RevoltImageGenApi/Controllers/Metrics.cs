using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace RevoltImageGenApi.Controllers
{
    [Route("api/[controller]")]
    public class Metrics : Controller
    {
        private static readonly IMongoCollection<BsonDocument> telemetryCollection = Utils.database.GetCollection<BsonDocument>(ConfigAndDefaults.mongoEndpointTelemetryCollection);
        private static readonly IMongoCollection<BsonDocument> logCollection = Utils.database.GetCollection<BsonDocument>(ConfigAndDefaults.mongoEndpointLogCollection);

        [HttpGet]
        public async Task<IActionResult> GetMetrics()
        {
            var results = await telemetryCollection.Find(_ => true).ToListAsync();

            var dict = results.ToDictionary(
                doc => doc["endpoint"].AsString,
                doc => doc["count"].AsInt32
            );

            return Ok(dict);
        }

        [HttpGet]
        [Route("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var pipeline = new[]
                {
                    new BsonDocument("$project", new BsonDocument
                    {
                        { "endpoint", 1 },
                        { "success", new BsonDocument("$cond", new BsonArray { new BsonDocument("$lt", new BsonArray { "$statusCode", 400 }), 1, 0 }) },
                        { "failed", new BsonDocument("$cond", new BsonArray { new BsonDocument("$gte", new BsonArray { "$statusCode", 400 }), 1, 0 }) },
                        { "timestamp", new BsonDocument("$dateTrunc", new BsonDocument
                            {
                                { "date", "$timestamp" },
                                { "unit", "minute" }
                            })
                        }
                    }),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", new BsonDocument
                            {
                                { "endpoint", "$endpoint" },
                                { "timestamp", "$timestamp" }
                            }},
                        { "count", new BsonDocument("$sum", 1) },
                        { "success", new BsonDocument("$sum", "$success") },
                        { "failed", new BsonDocument("$sum", "$failed") }
                    }),
                    new BsonDocument("$sort", new BsonDocument
                    {
                        { "_id.timestamp", 1 }
                    })
                };

            var results = await logCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            var logs = results.Select(r => new
            {
                endpoint = r["_id"]["endpoint"].AsString,
                timestamp = r["_id"]["timestamp"].ToUniversalTime(),
                count = r["count"].AsInt32,
                success = r["success"].AsInt32,
                failed = r["failed"].AsInt32
            });

            return Ok(new { logs });
        }
    }
}
