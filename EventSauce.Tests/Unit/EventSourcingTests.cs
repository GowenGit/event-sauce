using System;
using System.Text.Json;
using EventSauce.Core;
using Xunit;

namespace EventSauce.Tests.Unit
{
    public class EventSourcingTests
    {
        internal record UserCreatedEvent : SaucyEvent
        {
            public string Email { get; init; } = string.Empty;

            public string AuthId { get; init; } = string.Empty;
        }

        internal record UserId : SaucyAggregateId
        {
            public UserId(Guid id)
            {
                Kind = "User";
                Id = id;
            }

            public static UserId NewUser()
            {
                return new (Guid.NewGuid());
            }
        }

        [Fact]
        public void AggregateIdBase_ShouldBeCompared_BasedOnProperties()
        {
            var guid = Guid.NewGuid();

            var idOne = new UserId(guid);

            var idTwo = new UserId(guid);

            Assert.Equal(idOne, idTwo);

            idTwo = idTwo with
            {
                Id = Guid.NewGuid()
            };

            Assert.NotEqual(idOne, idTwo);
        }

        [Fact]
        public void DomainEvents_ShouldBeCompared_BasedOnProperties()
        {
            var eventOne = new UserCreatedEvent
            {
                Email = "joseph@gmail.com",
                AggregateId = UserId.NewUser(),
                AggregateVersion = 1
            };

            var eventTwo = eventOne with
            {
                Email = "adolf@gmail.com",
                AggregateId = UserId.NewUser(),
                AggregateVersion = 2
            };

            Assert.NotEqual(eventOne, eventTwo);
        }

        [Fact]
        public void DomainEvents_ShouldBeSerialized_WithoutBaseProperties()
        {
            var domainEvent = new UserCreatedEvent
            {
                Email = "joseph@gmail.com",
                AggregateId = UserId.NewUser(),
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
