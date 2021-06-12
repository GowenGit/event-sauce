using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EventSauce.Tests")]

namespace EventSauce.Postgre
{
    internal static class PostgreSqlCommands
    {
        public const string SetupTablesCommand = @"
CREATE TABLE IF NOT EXISTS {0} (
    AggregateId uuid NOT NULL,
    AggregateIdType varchar(256) NOT NULL,
    AggregateVersion int8 NOT NULL,
    Created timestamp(0) NOT NULL,
    EventId uuid NOT NULL UNIQUE,
    EventType varchar(1024) NOT NULL,
    EventData jsonb NOT NULL,
    PerformedBy uuid NOT NULL,
    UNIQUE (AggregateIdType,AggregateId,AggregateVersion)
);

CREATE INDEX IF NOT EXISTS {0}_aggregate_idx ON {0}(AggregateIdType,AggregateId);
";

        public const string InsertEvent = @"
INSERT INTO {0}
(
    AggregateId, AggregateIdType, AggregateVersion, Created, EventId, EventType, EventData, PerformedBy
)
VALUES
(
    @aggregate_id, @aggregate_id_type, @aggregate_version, @created, @event_id, @event_type, @event_data, @performed_by
)
";

        public const string SelectEvent = @"
SELECT * FROM {0}
WHERE AggregateId = @aggregate_id AND AggregateIdType = @aggregate_id_type;
";
    }
}
