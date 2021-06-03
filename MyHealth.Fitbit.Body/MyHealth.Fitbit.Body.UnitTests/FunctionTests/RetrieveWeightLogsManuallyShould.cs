using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyHealth.Common;
using MyHealth.Fitbit.Body.Functions;
using MyHealth.Fitbit.Body.Models;
using MyHealth.Fitbit.Body.Services;
using MyHealth.Fitbit.Body.Validators;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using mdl = MyHealth.Common.Models;

namespace MyHealth.Fitbit.Body.UnitTests.FunctionTests
{
    public class RetrieveWeightLogsManuallyShould
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IFitbitApiService> _mockFitbitApiService;
        private Mock<IMapper> _mockMapper;
        private Mock<IServiceBusHelpers> _mockServiceBusHelpers;
        private Mock<IDateValidator> _mockDateValidator;
        private Mock<HttpRequest> _mockHttpRequest;
        private Mock<ILogger> _mockLogger;

        private RetrieveWeightLogsManually _func;

        public RetrieveWeightLogsManuallyShould()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockFitbitApiService = new Mock<IFitbitApiService>();
            _mockMapper = new Mock<IMapper>();
            _mockServiceBusHelpers = new Mock<IServiceBusHelpers>();
            _mockDateValidator = new Mock<IDateValidator>();
            _mockHttpRequest = new Mock<HttpRequest>();
            _mockLogger = new Mock<ILogger>();

            _func = new RetrieveWeightLogsManually(
                _mockConfiguration.Object,
                _mockFitbitApiService.Object,
                _mockMapper.Object,
                _mockServiceBusHelpers.Object,
                _mockDateValidator.Object);
        }

        [Fact]
        public async Task ReturnOkResultWhenMappedWeightLogsAreSentToBodyTopic()
        {
            // Arrange
            var startweightLog = new mdl.Weight
            {
                Date = "2019-12-01"
            };
            var endWeightLog = new mdl.Weight
            {
                Date = "2019-12-31"
            };
            var weightResponse = new WeightResponseObject
            {
                weight = new List<Weight>()
                {
                }
            };

            weightResponse.weight.Add(new Weight() { date = startweightLog.Date });
            weightResponse.weight.Add(new Weight() { date = endWeightLog.Date });
            _mockDateValidator.Setup(x => x.IsDateValid(It.IsAny<string>())).Returns(true);
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(weightResponse);
            _mockMapper.Setup(x => x.Map(It.IsAny<WeightResponseObject>(), It.IsAny<mdl.Weight>())).Verifiable();
            _mockServiceBusHelpers.Setup(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>())).Returns(Task.CompletedTask);

            // Act
            var response = await _func.Run(_mockHttpRequest.Object, _mockLogger.Object, startweightLog.Date, endWeightLog.Date);

            // Assert
            Assert.Equal(typeof(OkResult), response.GetType());
            _mockServiceBusHelpers.Verify(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>()), Times.Exactly(2));
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [Theory]
        [InlineData("2020-12-100", "2020-12-10")]
        [InlineData("2020-111-12", "2020-11-01")]
        [InlineData("20201-12-11", "2020-12-01")]
        [InlineData("2020-12-10", "2020-12-100")]
        [InlineData("2020-11-12", "2020-111-01")]
        [InlineData("2020-12-11", "20201-12-01")]
        public async Task ReturnBadRequestResultWhenProvidedStartAndOrEndDateIsInvalid(string startDate, string endDate)
        {
            // Arrange
            var weightResponse = new WeightResponseObject
            {
                weight = new List<Weight>()
                {
                }
            };
            byte[] byteArray = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(weightResponse));
            MemoryStream memoryStream = new MemoryStream(byteArray);
            _mockHttpRequest.Setup(r => r.Body).Returns(memoryStream);

            // Act
            var response = await _func.Run(_mockHttpRequest.Object, _mockLogger.Object, startDate, endDate);

            // Assert
            Assert.Equal(typeof(BadRequestObjectResult), response.GetType());
            _mockServiceBusHelpers.Verify(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>()), Times.Never);
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public async Task ReturnNotFoundResultWhenNoWeightResponseIsFound()
        {
            // Arrange
            var startweightLog = new mdl.Weight
            {
                Date = "2019-12-01"
            };
            var endWeightLog = new mdl.Weight
            {
                Date = "2019-12-31"
            };
            var weightResponse = new WeightResponseObject
            {
                weight = new List<Weight>()
                {
                }
            };
            weightResponse.weight.Add(new Weight() { date = startweightLog.Date });
            weightResponse.weight.Add(new Weight() { date = endWeightLog.Date });

            _mockDateValidator.Setup(x => x.IsDateValid(It.IsAny<string>())).Returns(true);
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<WeightResponseObject>(null));

            // Act
            var response = await _func.Run(_mockHttpRequest.Object, _mockLogger.Object, startweightLog.Date, endWeightLog.Date);

            // Assert
            Assert.Equal(typeof(NotFoundObjectResult), response.GetType());
            _mockServiceBusHelpers.Verify(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>()), Times.Never);
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public async Task Throw500WhenMappingWeightResponseToWeightObjectFails()
        {
            // Arrange
            var startweightLog = new mdl.Weight
            {
                Date = "2019-12-01"
            };
            var endWeightLog = new mdl.Weight
            {
                Date = "2019-12-31"
            };
            var weightResponse = new WeightResponseObject
            {
                weight = new List<Weight>()
                {
                }
            };
            weightResponse.weight.Add(new Weight() { date = startweightLog.Date });
            weightResponse.weight.Add(new Weight() { date = endWeightLog.Date });

            _mockDateValidator.Setup(x => x.IsDateValid(It.IsAny<string>())).Returns(true);
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(weightResponse);
            _mockMapper.Setup(x => x.Map(It.IsAny<Weight>(), It.IsAny<mdl.Weight>())).Throws(new Exception());

            // Act
            var response = await _func.Run(_mockHttpRequest.Object, _mockLogger.Object, startweightLog.Date, endWeightLog.Date);

            // Assert
            Assert.Equal(typeof(StatusCodeResult), response.GetType());
            var responseAsStatusCodeResult = (StatusCodeResult)response;
            Assert.Equal(500, responseAsStatusCodeResult.StatusCode);
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task Throw500WhenSendingToBodyTopicFails()
        {
            // Arrange
            var startweightLog = new mdl.Weight
            {
                Date = "2019-12-01"
            };
            var endWeightLog = new mdl.Weight
            {
                Date = "2019-12-31"
            };
            var weightResponse = new WeightResponseObject
            {
                weight = new List<Weight>()
                {
                }
            };
            weightResponse.weight.Add(new Weight() { date = startweightLog.Date });
            weightResponse.weight.Add(new Weight() { date = endWeightLog.Date });

            _mockDateValidator.Setup(x => x.IsDateValid(It.IsAny<string>())).Returns(true);
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(weightResponse);
            _mockMapper.Setup(x => x.Map(It.IsAny<Weight>(), It.IsAny<mdl.Weight>())).Verifiable();
            _mockServiceBusHelpers.Setup(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>())).ThrowsAsync(new Exception());

            // Act
            var response = await _func.Run(_mockHttpRequest.Object, _mockLogger.Object, startweightLog.Date, endWeightLog.Date);

            // Assert
            Assert.Equal(typeof(StatusCodeResult), response.GetType());
            var responseAsStatusCodeResult = (StatusCodeResult)response;
            Assert.Equal(500, responseAsStatusCodeResult.StatusCode);
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }
    }
}
