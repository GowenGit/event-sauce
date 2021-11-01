using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

#pragma warning disable CA2100

namespace EventSauce.MongoDB
{
    internal class MongoDBSauceStore : ISauceStore
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoDBSauceStore(IMongoCollection<BsonDocument> collection)
        {
            _collection = collection;
        }

        public async Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id)
        {
            try
            {
                var aggregateBson = id.ToBsonDocument();

                const string fieldName = nameof(SaucyEvent.AggregateId);

                using var cursor = await _collection.FindAsync(x => x[fieldName] == aggregateBson);

                var result = new List<SaucyEvent>();

                while (await cursor.MoveNextAsync())
                {
                    result.AddRange(cursor.Current.Select(x => BsonSerializer.Deserialize<SaucyEvent>(x)));
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new EventSauceMongoDBException($"Failed to fetch events for {id}", ex);
            }
        }

        public async Task AppendEvent(SaucyEvent sourceEvent, SaucyAggregateId? performedBy)
        {
            try
            {
                if (sourceEvent.AggregateId == null)
                {
                    throw new ArgumentNullException(nameof(sourceEvent.AggregateId));
                }

                var bsonDocument = sourceEvent.ToBsonDocument(sourceEvent.GetType());

                if (performedBy != null)
                {
                    bsonDocument.Add("_performedBy", performedBy.ToBsonDocument());
                }

                await _collection.InsertOneAsync(bsonDocument);
            }
            catch (Exception ex)
            {
                throw new EventSauceMongoDBException($"Failed to insert event {sourceEvent}", ex);
            }
        }

        public void Dispose() { }
    }
}