using AutoMapper;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyHealth.Common;
using MyHealth.Fitbit.Body.Functions;
using mod = MyHealth.Fitbit.Body.Models;
using MyHealth.Fitbit.Body.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using mdl = MyHealth.Common.Models;

namespace MyHealth.Fitbit.Body.UnitTests.FunctionTests
{
    public class GetMonthlyWeightLogsShould
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IFitbitApiService> _mockFitbitApiService;
        private Mock<IMapper> _mockMapper;
        private Mock<IServiceBusHelpers> _mockServiceBusHelpers;
        private Mock<ILogger> _mockLogger;
        private TimerInfo _testTimerInfo;

        private GetMonthlyWeightLogs _func;

        public GetMonthlyWeightLogsShould()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockFitbitApiService = new Mock<IFitbitApiService>();
            _mockMapper = new Mock<IMapper>();
            _mockServiceBusHelpers = new Mock<IServiceBusHelpers>();
            _mockLogger = new Mock<ILogger>();
            _testTimerInfo = default(TimerInfo);

            _func = new GetMonthlyWeightLogs(
                _mockConfiguration.Object,
                _mockFitbitApiService.Object,
                _mockMapper.Object,
                _mockServiceBusHelpers.Object);
        }

        [Fact]
        public async Task RetrieveFoodLogResponseAndSendMappedObjectToBodyTopic()
        {
            // Arrange           
            var testWeight = new mod.Weight { bmi = 26.5, date = "2020-05-10", fat = 10.0, weight = 90.0, logId = 1, source = "Test", time = DateTime.UtcNow.ToString() };
            var weightResponseObject = new mod.WeightResponseObject { weight = new List<mod.Weight>() { testWeight } };
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(weightResponseObject);
            _mockMapper.Setup(x => x.Map(It.IsAny<mod.Weight>(), It.IsAny<mdl.Weight>())).Verifiable();
            _mockServiceBusHelpers.Setup(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>())).Returns(Task.CompletedTask);

            // Act
            Func<Task> getDailyFoodLogAction = async () => await _func.Run(_testTimerInfo, _mockLogger.Object);

            // Assert
            await getDailyFoodLogAction.Should().NotThrowAsync<Exception>();
            _mockServiceBusHelpers.Verify(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>()), Times.Once);
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public async Task ThrowAndCatchExceptionWhenFitApiServiceThrowsException()
        {
            // Arrange
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception());

            // Act
            Func<Task> getDailySleepAction = async () => await _func.Run(_testTimerInfo, _mockLogger.Object);

            // Assert
            await getDailySleepAction.Should().ThrowAsync<Exception>();
            _mockServiceBusHelpers.Verify(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>()), Times.Never);
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task ThrowAndCatchExceptionWhenMapperThrowsException()
        {
            // Arrange
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(It.IsAny<mod.WeightResponseObject>());
            _mockMapper.Setup(x => x.Map(It.IsAny<mod.WeightResponseObject>(), It.IsAny<mdl.Weight>())).Throws(new Exception());

            // Act
            Func<Task> getDailySleepAction = async () => await _func.Run(_testTimerInfo, _mockLogger.Object);

            // Assert
            await getDailySleepAction.Should().ThrowAsync<Exception>();
            _mockServiceBusHelpers.Verify(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>()), Times.Never);
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async Task ThrowAndCatchExceptionWhenSendMessageToTopicThrowsException()
        {
            // Arrange
            _mockFitbitApiService.Setup(x => x.GetWeightLogs(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(It.IsAny<mod.WeightResponseObject>());
            _mockMapper.Setup(x => x.Map(It.IsAny<mod.WeightResponseObject>(), It.IsAny<mdl.Weight>())).Verifiable();
            _mockServiceBusHelpers.Setup(x => x.SendMessageToTopic(It.IsAny<string>(), It.IsAny<mdl.Weight>())).ThrowsAsync(new Exception());

            // Act
            Func<Task> getDailySleepAction = async () => await _func.Run(_testTimerInfo, _mockLogger.Object);

            // Assert
            await getDailySleepAction.Should().ThrowAsync<Exception>();
            _mockServiceBusHelpers.Verify(x => x.SendMessageToQueue(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }
    }
}
