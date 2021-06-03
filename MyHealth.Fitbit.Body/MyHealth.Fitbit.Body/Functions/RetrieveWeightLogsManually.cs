using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using MyHealth.Fitbit.Body.Services;
using MyHealth.Fitbit.Body.Validators;
using System;
using System.Threading.Tasks;
using mdl = MyHealth.Common.Models;

namespace MyHealth.Fitbit.Body.Functions
{
    public class RetrieveWeightLogsManually
    {
        private readonly IConfiguration _configuration;
        private readonly IFitbitApiService _fitbitApiService;
        private readonly IMapper _mapper;
        private readonly IServiceBusHelpers _serviceBusHelpers;
        private readonly IDateValidator _dateValidator;

        public RetrieveWeightLogsManually(
            IConfiguration configuration,
            IFitbitApiService fitbitApiService,
            IMapper mapper,
            IServiceBusHelpers serviceBusHelpers,
            IDateValidator dateValidator)
        {
            _configuration = configuration;
            _fitbitApiService = fitbitApiService;
            _mapper = mapper;
            _serviceBusHelpers = serviceBusHelpers;
            _dateValidator = dateValidator;
        }

        [FunctionName(nameof(RetrieveWeightLogsManually))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "WeightLog/{startDate}/{endDate}")] HttpRequest req,
            ILogger log,
            string startDate,
            string endDate)
        {
            IActionResult result;

            try
            {
                bool isStartDateValid = _dateValidator.IsDateValid(startDate);
                if (isStartDateValid == false)
                {
                    result = new BadRequestObjectResult($"The provided start date: {startDate} format is invalid. Please supply a date in the following format: yyyy-MM-dd");
                    return result;
                }

                bool isEndDateValid = _dateValidator.IsDateValid(endDate);
                if (isEndDateValid == false)
                {
                    result = new BadRequestObjectResult($"The provided end date: {endDate} format is invalid. Please supply a date in the following format: yyyy-MM-dd");
                }

                log.LogInformation($"Attempting to retreive weight logs for the period {startDate} to {endDate}");
                var weightResponse = await _fitbitApiService.GetWeightLogs(startDate, endDate);
                if (weightResponse == null)
                {
                    result = new NotFoundObjectResult($"No weight logs found for the period {startDate} to {endDate}");
                    return result;
                }

                log.LogInformation("Mapping API responses to Weight objects");
                foreach (var record in weightResponse.weight)
                {
                    var weight = new mdl.Weight();
                    _mapper.Map(record, weight);
                    await _serviceBusHelpers.SendMessageToTopic(_configuration["BodyTopic"], weight);
                }

                result = new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError($"Exception thrown in {nameof(RetrieveWeightLogsManually)}: {ex.Message}");
                await _serviceBusHelpers.SendMessageToQueue(_configuration["ExceptionQueue"], ex);
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }
    }
}
