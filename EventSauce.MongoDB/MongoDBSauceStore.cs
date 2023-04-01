using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace EventSauce.MongoDB
{
    internal class MongoDBSauceStore : ISauceStore
    {
        private readonly IMongoCollection<BsonDocument> _collection;

        public MongoDBSauceStore(IMongoCollection<BsonDocument> collection)
        {
            _collection = collection;
        }

        public async Task<IEnumerable<SaucyEvent<TAggregateId>>> ReadEvents<TAggregateId>(TAggregateId id)
        {
            try
            {
                const string fieldName = nameof(SaucyEvent<TAggregateId>.AggregateId);

                var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TAggregateId>();

                var value = serializer.ToBsonValue(id);

                using var cursor = await _collection.FindAsync(x => x[fieldName] == value);

                var result = new List<SaucyEvent<TAggregateId>>();

                while (await cursor.MoveNextAsync())
                {
                    result.AddRange(cursor.Current.Select(x => BsonSerializer.Deserialize<SaucyEvent<TAggregateId>>(x)));
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new EventSauceMongoDBException($"Failed to fetch events for {id}", ex);
            }
        }

        public async Task AppendEvent<TAggregateId>(SaucyEvent<TAggregateId> sourceEvent, object? performedBy)
        {
            try
            {
                if (sourceEvent.AggregateId == null)
                {
                    throw new ArgumentNullException(nameof(sourceEvent.AggregateId));
                }

                var bsonDocument = sourceEvent.ToBsonDocument();

                bsonDocument.Add("_id", ObjectId.GenerateNewId());

                if (performedBy != null)
                {
                    var serializer = BsonSerializer.SerializerRegistry.GetSerializer(performedBy.GetType());

                    bsonDocument.Add("_performedBy", serializer.ToBsonValue(performedBy));
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