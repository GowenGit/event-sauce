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
        private readonly Dictionary<string, Type> _aggregateTypes;

        private readonly JsonSerializerOptions _options;

        public PostgreSauceStore(
            NpgsqlConnection connection,
            string tableName,
            Dictionary<string, Type> eventTypes,
            Dictionary<string, Type> aggregateTypes,
            JsonSerializerOptions options)
        {
            _connection = connection;
            _tableName = tableName;
            _eventTypes = eventTypes;
            _aggregateTypes = aggregateTypes;
            _options = options;
        }

        public async Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id)
        {
            try
            {
                var sql = GetCommand(PostgreSqlCommands.SelectEvent);

                await using var command = new NpgsqlCommand(sql, _connection);

                command.Parameters.AddWithValue("aggregate_id", id.Id);
                command.Parameters.AddWithValue("aggregate_id_type", id.IdType);

                await using var reader = await command.ExecuteReaderAsync();

                var result = new List<SaucyEvent>();

                SaucyAggregateId? aggregate = null;

                var eventTypeIndex = reader.GetOrdinal("EventType");
                var eventDataIndex = reader.GetOrdinal("EventData");
                var eventIdIndex = reader.GetOrdinal("EventId");
                var createdIndex = reader.GetOrdinal("Created");
                var aggregateVersionIndex = reader.GetOrdinal("AggregateVersion");
                var aggregateIdIndex = reader.GetOrdinal("AggregateId");
                var aggregateIdTypeNameIndex = reader.GetOrdinal("AggregateIdType");

                while (reader.Read())
                {
                    var eventTypeName = reader.GetSaucyString(eventTypeIndex);
                    var eventData = reader.GetSaucyString(eventDataIndex);
                    var eventId = reader.GetSaucyGuid(eventIdIndex);
                    var created = reader.GetSaucyDate(createdIndex);
                    var aggregateVersion = reader.GetSaucyLong(aggregateVersionIndex);

                    var eventType = _eventTypes[eventTypeName];

                    if (aggregate == null)
                    {
                        var aggregateId = reader.GetSaucyGuid(aggregateIdIndex);
                        var aggregateIdTypeName = reader.GetSaucyString(aggregateIdTypeNameIndex);
                        var aggregateIdType = _aggregateTypes[aggregateIdTypeName];
                        aggregate = (SaucyAggregateId)Activator.CreateInstance(aggregateIdType, aggregateId)!;
                    }

                    var sourceEvent = (SaucyEvent)JsonSerializer.Deserialize(eventData, eventType, _options)!;

                    result.Add(sourceEvent with
                    {
                        AggregateVersion = aggregateVersion,
                        AggregateId = aggregate,
                        Id = eventId,
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

        public async Task AppendEvent(SaucyEvent sourceEvent, SaucyAggregateId? performedBy)
        {
            try
            {
                var eventData = JsonSerializer.Serialize(sourceEvent, sourceEvent.GetType(), _options);

                var eventType = sourceEvent.GetType().Name;

                var sql = GetCommand(PostgreSqlCommands.InsertEvent);

                await using var command = new NpgsqlCommand(sql, _connection);

                command.Parameters.AddWithValue("aggregate_id", sourceEvent.AggregateId?.Id ?? throw new ArgumentNullException(nameof(sourceEvent.AggregateId)));
                command.Parameters.AddWithValue("aggregate_id_type", sourceEvent.AggregateId?.IdType ?? throw new ArgumentNullException(nameof(sourceEvent.AggregateId)));
                command.Parameters.AddWithValue("aggregate_version", sourceEvent.AggregateVersion);
                command.Parameters.AddWithValue("created", sourceEvent.Created);
                command.Parameters.AddWithValue("event_id", sourceEvent.Id);
                command.Parameters.AddWithValue("event_type", eventType);
                command.Parameters.AddWithValue("event_data", NpgsqlDbType.Jsonb, eventData);
                command.Parameters.AddWithValue("performed_by", performedBy?.Id ?? Guid.Empty);

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