using EventSauce.Extensions.Microsoft.DependencyInjection;
using EventSauce.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using Xunit;

#pragma warning disable CA1711

namespace EventSauce.Tests.Integration
{
    public sealed class MongoDBStorageFixture : IDisposable
    {
        private readonly ServiceProvider _provider;

        public MongoDBStorageFixture()
        {
            var objectIdSerializer = BsonSerializer.SerializerRegistry.GetSerializer<ObjectId>();
            BsonSerializer.RegisterSerializer(new UserSerializer(objectIdSerializer));

            var services = new ServiceCollection();

            const string connectionString =
                "mongodb://root:password@localhost:27017/";

            services.AddEventSauce(options =>
            {
                var factory = new MongoDBSauceStoreFactory(
                    new[]
                    {
                        typeof(MongoDBTests).Assembly
                    },
                    connectionString,
                    "sauce");

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

    [CollectionDefinition("MongoDB storage collection")]
    public class MongoDBStorageCollection : ICollectionFixture<MongoDBStorageFixture> { }

    [Collection("MongoDB storage collection")]
    public class MongoDBTests
    {
        private readonly MongoDBStorageFixture _fixture;

        public MongoDBTests(MongoDBStorageFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Aggregate_WhenCreatedAndSaved_ShouldHaveNoUncommittedEvents()
        {
            var userId = User.New();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate<User>(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = _fixture.GetSutObject();

            await sut.Save<UserAggregate<User>, User>(user);

            var events = user.GetUncommittedEvents().ToList();

            Assert.Empty(events);

            Assert.Equal(userId, user.Id);
            Assert.Equal(newEmail, user.Email);
            Assert.Equal(1, user.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedAndSaved_ShouldRetrieve()
        {
            var userId = User.New();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate<User>(userId, email, auth);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate<User>, User>(user);

            repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser.Id);
            Assert.Equal(email, repoUser.Email);
            Assert.Equal(0, repoUser.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedAndSaved_ShouldRetrieve()
        {
            var userId = User.New();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate<User>(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate<User>, User>(user);

            repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser.Id);
            Assert.Equal(newEmail, repoUser.Email);
            Assert.Equal(1, repoUser.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedAndSavedWithPerformedBy_ShouldRetrieve()
        {
            var userId = User.New();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate<User>(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate<User>, User>(user, userId);

            repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser.Id);
            Assert.Equal(newEmail, repoUser.Email);
            Assert.Equal(1, repoUser.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedThenChangedToInvalidAndSavedWithPerformedBy_ShouldRetrieve()
        {
            var userId = User.New();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate<User>(userId, email, auth);

            const string newEmail = "changed@gmail.com";
            const string invalidEmail = "faker_email";

            user.ChangeEmail(newEmail);
            user.ChangeEmailToInvalid(invalidEmail);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<UserAggregate<User>, User>(user, userId);

            repoUser = await sut.GetById<UserAggregate<User>, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser.Id);
            Assert.Equal(invalidEmail, repoUser.Email);
            Assert.Equal(2, repoUser.Version);
        }

        [Fact]
        public async Task Aggregate_WhenInteractingWithCustomMongoAggregate_ShouldSucceed()
        {
            var userId = User.New();

            const string? email = "origina@gmail.com";

            var user = new CustomMongoUserAggregate(userId, email);

            var sut = _fixture.GetSutObject();

            var repoUser = await sut.GetById<CustomMongoUserAggregate, User>(userId);

            Assert.Null(repoUser);

            await sut.Save<CustomMongoUserAggregate, User>(user, userId);

            repoUser = await sut.GetById<CustomMongoUserAggregate, User>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser.Id);
            Assert.Equal(email, repoUser.Email);
            Assert.Equal(0, repoUser.Version);
        }
    }
}
