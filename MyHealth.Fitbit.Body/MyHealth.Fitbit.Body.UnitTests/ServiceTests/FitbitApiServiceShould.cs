using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using MyHealth.Common;
using MyHealth.Fitbit.Body.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MyHealth.Fitbit.Body.UnitTests.ServiceTests
{
    public class FitbitApiServiceShould
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IKeyVaultHelper> _mockKeyVaultHelper;
        private Mock<HttpClient> _mockHttpClient;
        private Mock<HttpResponseMessage> _mockResponseMessage;

        private FitbitApiService _sut;

        public FitbitApiServiceShould()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockKeyVaultHelper = new Mock<IKeyVaultHelper>();
            _mockHttpClient = new Mock<HttpClient>();
            _mockResponseMessage = new Mock<HttpResponseMessage>();

            _sut = new FitbitApiService(
                _mockConfiguration.Object,
                _mockKeyVaultHelper.Object,
                _mockHttpClient.Object);
        }

        [Fact]
        public async Task CatchAndThrowExceptionWhenRetrieveSecretFromKeyVaultThrowsException()
        {
            // Arrange
            _mockKeyVaultHelper.Setup(x => x.RetrieveSecretFromKeyVaultAsync(It.IsAny<string>())).ThrowsAsync(new Exception());

            // Act
            Func<Task> fitbitApiServiceAction = async () => await _sut.GetWeightLogs("2021-01-01", "2021-01-31");

            // Assert
            await fitbitApiServiceAction.Should().ThrowAsync<Exception>();
        }       
    }
}
