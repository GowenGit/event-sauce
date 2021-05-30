using EventSauce.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventSauce.Tests.Integration
{
    public class User : SaucyAggregate
    {
        private User() { }

        public string Email { get; private set; } = string.Empty;

        public User(UserId id, string email, string authId)
        {
            IssueEvent(new UserCreatedEvent
            {
                Email = email,
                AuthId = authId,
                AggregateId = id
            });
        }

        public void ChangeEmail(string email)
        {
            IssueEvent(new UserChangeEmailEvent
            {
                Email = email
            });
        }

        public void Apply(UserCreatedEvent domainEvent)
        {
            Email = domainEvent.Email;
        }

        public void Apply(UserChangeEmailEvent domainEvent)
        {
            Email = domainEvent.Email;
        }
    }

    public record UserCreatedEvent : SaucyEvent
    {
        public string Email { get; init; } = string.Empty;

        public string AuthId { get; init; } = string.Empty;
    }

    public record UserChangeEmailEvent : SaucyEvent
    {
        public string Email { get; init; } = string.Empty;
    }

    public record UserId : SaucyAggregateId
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

    public class InMemoryTests
    {
        private static DateTime PastDate() => DateTime.UtcNow.AddMinutes(-10);

        public static ISaucyRepository GetSutObject()
        {
            var services = new ServiceCollection();

            services.AddEventSauce();

            var provider = services.BuildServiceProvider();

            return provider.GetService<ISaucyRepository>() ?? throw new ArgumentNullException(nameof(ISaucyRepository));
        }

        [Fact]
        public void Aggregate_WhenCreated_ShouldHaveOneEvent()
        {
            var userId = UserId.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new User(userId, email, auth);

            var events = user.GetUncommittedEvents().ToList();

            Assert.Single(events);

            var createdEvent = events[0] as UserCreatedEvent;

            Assert.NotNull(createdEvent);

            Assert.Equal(userId, createdEvent?.AggregateId);
            Assert.Equal(email, createdEvent?.Email);
            Assert.Equal(auth, createdEvent?.AuthId);
            Assert.Equal(0, createdEvent?.AggregateVersion);

            Assert.NotEqual(Guid.Empty, createdEvent?.EventId);
            Assert.True(createdEvent?.Created > PastDate());

            Assert.Equal(userId, user.Id);
            Assert.Equal(email, user.Email);
            Assert.Equal(0, user.Version);
        }

        [Fact]
        public void Aggregate_WhenCreatedAndEmailChanged_ShouldHaveTwoEvents()
        {
            var userId = UserId.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new User(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var events = user.GetUncommittedEvents().ToList();

            Assert.Equal(2, events.Count);

            var createdEvent = events[0] as UserCreatedEvent;

            Assert.NotNull(createdEvent);

            Assert.Equal(userId, createdEvent?.AggregateId);
            Assert.Equal(email, createdEvent?.Email);
            Assert.Equal(auth, createdEvent?.AuthId);
            Assert.Equal(0, createdEvent?.AggregateVersion);

            Assert.NotEqual(Guid.Empty, createdEvent?.EventId);
            Assert.True(createdEvent?.Created > PastDate());

            var changeEmailEvent = events[1] as UserChangeEmailEvent;

            Assert.NotNull(createdEvent);

            Assert.Equal(userId, changeEmailEvent?.AggregateId);
            Assert.Equal(newEmail, changeEmailEvent?.Email);
            Assert.Equal(1, changeEmailEvent?.AggregateVersion);

            Assert.NotEqual(Guid.Empty, changeEmailEvent?.EventId);
            Assert.True(changeEmailEvent?.Created > PastDate());

            Assert.Equal(userId, user.Id);
            Assert.Equal(newEmail, user.Email);
            Assert.Equal(1, user.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedAndSaved_ShouldHaveNoUncommittedEvents()
        {
            var userId = UserId.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new User(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = GetSutObject();

            await sut.Save(user);

            var events = user.GetUncommittedEvents().ToList();

            Assert.Empty(events);

            Assert.Equal(userId, user.Id);
            Assert.Equal(newEmail, user.Email);
            Assert.Equal(1, user.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedAndSaved_ShouldRetrieve()
        {
            var userId = UserId.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new User(userId, email, auth);

            var sut = GetSutObject();

            var repoUser = await sut.GetById<User>(userId);

            Assert.Null(repoUser);

            await sut.Save(user);

            repoUser = await sut.GetById<User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(email, repoUser?.Email);
            Assert.Equal(0, repoUser?.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedAndSaved_ShouldRetrieve()
        {
            var userId = UserId.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new User(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = GetSutObject();

            var repoUser = await sut.GetById<User>(userId);

            Assert.Null(repoUser);

            await sut.Save(user);

            repoUser = await sut.GetById<User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(newEmail, repoUser?.Email);
            Assert.Equal(1, repoUser?.Version);
        }
    }
}
