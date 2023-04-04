using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventSauce.Postgre
{
    internal class PostgreSauceStore : ISauceStore
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _tableName;

        private readonly Dictionary<string, Type> _eventTypes;

        private readonly JsonSerializerOptions _options;

        public PostgreSauceStore(
            NpgsqlConnection connection,
            string tableName,
            Dictionary<string, Type> eventTypes,
            JsonSerializerOptions options)
        {
            _connection = connection;
            _tableName = tableName;
            _eventTypes = eventTypes;
            _options = options;
        }

        public async Task<IEnumerable<SaucyEvent<TAggregateId>>> ReadEvents<TAggregateId>(TAggregateId id)
        {
            try
            {
                var sql = GetCommand(PostgreSqlCommands.SelectEvent);

                await using var command = new NpgsqlCommand(sql, _connection);

                command.Parameters.AddWithValue("aggregate_id", id);
                command.Parameters.AddWithValue("aggregate_id_type", id.GetType().Name);

                await using var reader = await command.ExecuteReaderAsync();

                var result = new List<SaucyEvent<TAggregateId>>();

                var eventTypeIndex = reader.GetOrdinal("EventType");
                var eventDataIndex = reader.GetOrdinal("EventData");
                var createdIndex = reader.GetOrdinal("Created");
                var aggregateVersionIndex = reader.GetOrdinal("AggregateVersion");

                while (reader.Read())
                {
                    var eventTypeName = reader.GetSaucyString(eventTypeIndex);
                    var eventData = reader.GetSaucyString(eventDataIndex);
                    var created = reader.GetSaucyDate(createdIndex);
                    var aggregateVersion = reader.GetSaucyLong(aggregateVersionIndex);

                    var eventType = _eventTypes[eventTypeName];

                    if (eventType.IsGenericType)
                    {
                        eventType = eventType.MakeGenericType(typeof(TAggregateId));
                    }

                    var sourceEvent = (SaucyEvent<TAggregateId>)JsonSerializer.Deserialize(eventData, eventType, _options)!;

                    result.Add(sourceEvent with
                    {
                        AggregateVersion = aggregateVersion,
                        AggregateId = id,
                        Created = created
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new EventSaucePostgreException($"Failed to fetch events for {id}", ex);
            }
        }

        public async Task AppendEvent<TAggregateId>(SaucyEvent<TAggregateId> sourceEvent, object? performedBy)
        {
            try
            {
                var eventData = JsonSerializer.Serialize(sourceEvent, sourceEvent.GetType(), _options);

                var eventType = sourceEvent.GetType().Name;

                var sql = GetCommand(PostgreSqlCommands.InsertEvent);

                await using var command = new NpgsqlCommand(sql, _connection);

                command.Parameters.AddWithValue("aggregate_id", sourceEvent.AggregateId);
                command.Parameters.AddWithValue("aggregate_id_type", typeof(TAggregateId).Name);
                command.Parameters.AddWithValue("aggregate_version", sourceEvent.AggregateVersion);
                command.Parameters.AddWithValue("created", sourceEvent.Created);
                command.Parameters.AddWithValue("event_id", Guid.NewGuid());
                command.Parameters.AddWithValue("event_type", eventType);
                command.Parameters.AddWithValue("event_data", NpgsqlDbType.Jsonb, eventData);
                command.Parameters.AddWithValue("performed_by", performedBy ?? Guid.Empty);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new EventSaucePostgreException($"Failed to insert event {sourceEvent}", ex);
            }
        }

        private string GetCommand(string text)
        {
            return string.Format(text, _tableName);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
