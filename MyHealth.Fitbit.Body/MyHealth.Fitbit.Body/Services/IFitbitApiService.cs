using MyHealth.Fitbit.Body.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyHealth.Fitbit.Body.Services
{
    public interface IFitbitApiService
    {
        Task<WeightResponseObject> GetWeightLogs(string startDate, string endDate);
    }
}
