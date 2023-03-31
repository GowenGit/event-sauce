using EventSauce.Extensions.Microsoft.DependencyInjection;
using EventSauce.Postgre;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace EventSauce.Tests.Integration
{
    public sealed class PostgreStorageFixture : IDisposable
    {
        private readonly ServiceProvider _provider;

        public PostgreStorageFixture()
        {
            var services = new ServiceCollection();

            const string connectionString =
                "Server=localhost;Port=5433;Database=sauce;User Id=postgres;Password=password;";

            services.AddEventSauce(options =>
            {
                var factory = new PostgreSauceStoreFactory(
                    new[]
                    {
                        typeof(PostgreTests).Assembly
                    },
                    new JsonSerializerOptions
                    {
                        WriteIndented = false
                    },
                    connectionString);

                options.Store = _ => factory.Create();
            });

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

    [CollectionDefinition("PostgreSql storage collection")]
    public class PostgreStorageCollection : ICollectionFixture<PostgreStorageFixture> { }

    [Collection("PostgreSql storage collection")]
    public class PostgreTests
    {
        private readonly PostgreStorageFixture _fixture;

        public PostgreTests(PostgreStorageFixture fixture)
        {
            _fixture = fixture;
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

            await sut.Save<UserAggregate, User>(user);

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

            var repoUser = await sut.GetById<UserAggregate, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate, User>(user);

            repoUser = await sut.GetById<UserAggregate, User>(userId);

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

            var repoUser = await sut.GetById<UserAggregate, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate, User>(user);

            repoUser = await sut.GetById<UserAggregate, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(newEmail, repoUser?.Email);
            Assert.Equal(1, repoUser?.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedAndSavedWithPerformedBy_ShouldRetrieve()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate, User>(user, userId);

            repoUser = await sut.GetById<UserAggregate, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(newEmail, repoUser?.Email);
            Assert.Equal(1, repoUser?.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedThenChangedToInvalidAndSavedWithPerformedBy_ShouldRetrieve()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            const string newEmail = "changed@gmail.com";
            const string invalidEmail = "faker_email";

            user.ChangeEmail(newEmail);
            user.ChangeEmailToInvalid(invalidEmail);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate, User>(user, userId);

            repoUser = await sut.GetById<UserAggregate, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(invalidEmail, repoUser?.Email);
            Assert.Equal(2, repoUser?.Version);
        }
    }
}
