using System;
using System.Text.Json;
using Xunit;

namespace EventSauce.Tests.Unit
{
    public class EventSourcingTests
    {
        internal record UnitUserCreatedEvent : SaucyEvent<UnitUser>
        {
            public string Email { get; init; } = string.Empty;

            public string AuthId { get; init; } = string.Empty;
        }

        internal record UnitUser : SaucyAggregateId
        {
            public UnitUser(Guid id) : base(id) { }

            public static UnitUser NewUser()
            {
                return new (Guid.NewGuid());
            }
        }

        [Fact]
        public void AggregateIdBase_ShouldBeCompared_BasedOnProperties()
        {
            var guid = Guid.NewGuid();

            var idOne = new UnitUser(guid);

            var idTwo = new UnitUser(guid);

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
            var eventOne = new UnitUserCreatedEvent
            {
                Email = "joseph@gmail.com",
                AggregateId = UnitUser.NewUser(),
                AggregateVersion = 1
            };

            var eventTwo = eventOne with
            {
                Email = "adolf@gmail.com",
                AggregateId = UnitUser.NewUser(),
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
                AggregateId = UnitUser.NewUser(),
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
