namespace HappyPaws.Tests.Integration;

// Declares a single shared fixture for the entire integration test suite. All
// test classes tagged with [Collection("Integration")] share one
// CustomWebApplicationFactory, so only one PostgreSQL and one MinIO container
// starts per test run. This prevents the port-exhaustion and race conditions
// that occur when every IClassFixture spins up its own containers concurrently.
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>;
