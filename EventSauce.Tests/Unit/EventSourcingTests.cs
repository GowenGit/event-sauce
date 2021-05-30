using System;
using System.Text.Json;
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

        internal record User : SaucyAggregateId
        {
            public User(Guid id)
            {
                Id = id;
            }

            public static User NewUser()
            {
                return new (Guid.NewGuid());
            }
        }

        [Fact]
        public void AggregateIdBase_ShouldBeCompared_BasedOnProperties()
        {
            var guid = Guid.NewGuid();

            var idOne = new User(guid);

            var idTwo = new User(guid);

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
                AggregateId = User.NewUser(),
                AggregateVersion = 1
            };

            var eventTwo = eventOne with
            {
                Email = "adolf@gmail.com",
                AggregateId = User.NewUser(),
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
                AggregateId = User.NewUser(),
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
