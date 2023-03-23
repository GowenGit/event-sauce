using EventSauce.Extensions.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EventSauce.Tests.Integration
{
    public sealed class InMemoryStorageFixture : IDisposable
    {
        private readonly ServiceProvider _provider;

        public InMemoryStorageFixture()
        {
            var services = new ServiceCollection();

            services.AddEventSauce();

            _provider = services.BuildServiceProvider();
        }

        public ISaucyRepository GetSutObject()
        {
            return _provider.GetService<ISaucyRepository>() ?? throw new ArgumentNullException(nameof(ISaucyRepository));
        }

        public void Dispose()
        {
            _provider.Dispose();
        }
    }

    [CollectionDefinition("InMemory storage collection")]
    public class InMemoryStorageCollection : ICollectionFixture<InMemoryStorageFixture> { }

    [Collection("InMemory storage collection")]
    public class InMemoryTests
    {
        private static DateTime PastDate() => DateTime.UtcNow.AddMinutes(-10);

        private readonly InMemoryStorageFixture _fixture;

        public InMemoryTests(InMemoryStorageFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Aggregate_WhenCreated_ShouldHaveOneEvent()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            var events = user.GetUncommittedEvents().ToList();

            Assert.Single(events);

            var createdEvent = events[0] as UserCreatedEvent;

            Assert.NotNull(createdEvent);

            Assert.Equal(userId, createdEvent?.AggregateId);
            Assert.Equal(email, createdEvent?.Email);
            Assert.Equal(auth, createdEvent?.AuthId);
            Assert.Equal(0, createdEvent?.AggregateVersion);

            Assert.NotEqual(Guid.Empty, createdEvent?.Id);
            Assert.True(createdEvent?.Created > PastDate());

            Assert.Equal(userId, user.Id);
            Assert.Equal(email, user.Email);
            Assert.Equal(0, user.Version);
        }

        [Fact]
        public void Aggregate_WhenCreatedAndEmailChanged_ShouldHaveTwoEvents()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

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

            Assert.NotEqual(Guid.Empty, createdEvent?.Id);
            Assert.True(createdEvent?.Created > PastDate());

            var changeEmailEvent = events[1] as UserChangeEmailEvent;

            Assert.NotNull(createdEvent);

            Assert.Equal(userId, changeEmailEvent?.AggregateId);
            Assert.Equal(newEmail, changeEmailEvent?.Email);
            Assert.Equal(1, changeEmailEvent?.AggregateVersion);

            Assert.NotEqual(Guid.Empty, changeEmailEvent?.Id);
            Assert.True(changeEmailEvent?.Created > PastDate());

            Assert.Equal(userId, user.Id);
            Assert.Equal(newEmail, user.Email);
            Assert.Equal(1, user.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedAndSaved_ShouldHaveNoUncommittedEvents()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = _fixture.GetSutObject();

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
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.Null(repoUser);

            await sut.Save(user);

            repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(email, repoUser?.Email);
            Assert.Equal(0, repoUser?.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedAndSaved_ShouldRetrieve()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.Null(repoUser);

            await sut.Save(user);

            repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(newEmail, repoUser?.Email);
            Assert.Equal(1, repoUser?.Version);
        }
    }
}
