using System;
using System.Text.Json;
using Xunit;

namespace EventSauce.Tests.Unit
{
    public class EventSourcingTests
    {
        internal record UnitUserCreatedEvent : SaucyEvent<Guid>
        {
            public string Email { get; init; } = string.Empty;

            public string AuthId { get; init; } = string.Empty;
        }

        [Fact]
        public void DomainEvents_ShouldBeCompared_BasedOnProperties()
        {
            var eventOne = new UnitUserCreatedEvent
            {
                Email = "joseph@gmail.com",
                AggregateId = Guid.NewGuid(),
                AggregateVersion = 1
            };

            var eventTwo = eventOne with
            {
                Email = "adolf@gmail.com",
                AggregateId = Guid.NewGuid(),
                AggregateVersion = 2
            };

            Assert.NotEqual(eventOne, eventTwo);
        }

        [Fact]
        public void DomainEvents_ShouldBeSerialized_WithoutBaseProperties()
        {
            var domainEvent = new UnitUserCreatedEvent
            {
                Email = "joseph@gmail.com",
                AggregateId = Guid.NewGuid(),
                AggregateVersion = 1,
                AuthId = "some random id"
            };

            var data = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            Assert.Equal("{\"Email\":\"joseph@gmail.com\",\"AuthId\":\"some random id\"}", data);
        }
    }
}
