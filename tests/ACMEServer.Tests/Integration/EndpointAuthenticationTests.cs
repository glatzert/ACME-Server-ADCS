using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Endpoints;

namespace Th11s.ACMEServer.Tests.Integration
{
    [CollectionDefinition(DisableParallelization = true)]
    public class EndpointAuthenticationTestsCollection : ICollectionFixture<DefaultWebApplicationFactory>
    { }

    [Collection(typeof(EndpointAuthenticationTestsCollection))]
    public class EndpointAuthenticationTests : IClassFixture<DefaultWebApplicationFactory>
    {
        private readonly DefaultWebApplicationFactory _factory;

        public EndpointAuthenticationTests(DefaultWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// This contains all endpoints, that do not require authentication.
        /// </summary>
        public static string[] _unauthorizedEndpoints = [
            EndpointNames.Directory,
            EndpointNames.DirectoryAlt,
            EndpointNames.ProfileMetadata,
            EndpointNames.NewNonce,
            EndpointNames.NewNonceHead
        ];

        /// <summary>
        /// This contains all endpoints, that do not require an account to be accessed. This is used to verify that the endpoints have the correct authorization policies applied.
        /// </summary>
        public static string[] _accountlessEndpoints = [
            EndpointNames.NewAccount,
            EndpointNames.RevokeCert,
            "TestEndpoint"
        ];

        [Fact]
        public void Endpoints_Have_Valid_Authentication_Policies()
        {
            // Collect all endpoints from the application
            var endpointDataSources = _factory.Server.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();
            var endpoints = endpointDataSources.SelectMany(x => x.Endpoints).ToList();

            foreach(var endpoint in endpoints)
            {
                var name = endpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName;
                Assert.NotNull(name);

                var authorizeData = endpoint.Metadata.GetMetadata<IAuthorizeData>();

                if (!_unauthorizedEndpoints.Contains(name))
                {
                    Assert.NotNull(authorizeData);

                    if (!_accountlessEndpoints.Contains(name))
                    {
                        Assert.Equal(Policies.ValidAccount, authorizeData.Policy);
                    }
                }
                else
                {
                    Assert.Null(authorizeData);
                }
            }
        }
    }
}
