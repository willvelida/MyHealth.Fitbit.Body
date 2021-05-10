using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using MyHealth.Fitbit.Body.Services;
using mdl = MyHealth.Common.Models;

namespace MyHealth.Fitbit.Body.Functions
{
    public class GetMonthlyWeightLogs
    {
        private readonly IConfiguration _configuration;
        private readonly IFitbitApiService _fitbitApiService;
        private readonly IMapper _mapper;
        private readonly IServiceBusHelpers _serviceBusHelpers;

        public GetMonthlyWeightLogs(
            IConfiguration configuration,
            IFitbitApiService fitbitApiService,
            IMapper mapper,
            IServiceBusHelpers serviceBusHelpers)
        {
            _configuration = configuration;
            _fitbitApiService = fitbitApiService;
            _mapper = mapper;
            _serviceBusHelpers = serviceBusHelpers;
        }

        [FunctionName(nameof(GetMonthlyWeightLogs))]
        public async Task Run([TimerTrigger("0 0 6 1 * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"{nameof(GetMonthlyWeightLogs)} executed at: {DateTime.Now}");
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                var endDate = new DateTime(startDate.Year, startDate.Month, DateTime.DaysInMonth(startDate.Year, startDate.Month));
                var startDateParameter = startDate.ToString("yyyy-MM-dd");
                var endDateParameter = endDate.ToString("yyyy-MM-dd");
                log.LogInformation($"Attempting to retrieve weight logs between {startDateParameter} and {endDateParameter}");

                var weightResponse = await _fitbitApiService.GetWeightLogs(startDateParameter, endDateParameter);

                foreach (var record in weightResponse.weight)
                {
                    var weight = new mdl.Weight();
                    _mapper.Map(record, weight);
                    await _serviceBusHelpers.SendMessageToTopic(_configuration["BodyTopic"], weight);
                }
                log.LogInformation($"Successfully mapped and sent {weightResponse.weight.Count} records to Service Bus.");
            }
            catch (Exception ex)
            {
                log.LogError($"Exception thrown in {nameof(GetMonthlyWeightLogs)}: {ex.Message}");
                await _serviceBusHelpers.SendMessageToQueue(_configuration["ExceptionQueue"], ex);
                throw ex;
            }
        }
    }
}
