using Microsoft.Extensions.Configuration;
using MyHealth.Common;
using MyHealth.Fitbit.Body.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MyHealth.Fitbit.Body.Services
{
    public class FitbitApiService : IFitbitApiService
    {
        private readonly IConfiguration _configuration;
        private readonly IKeyVaultHelper _keyVaultHelper;
        private HttpClient _httpClient;

        public FitbitApiService(
            IConfiguration configuration,
            IKeyVaultHelper keyVaultHelper,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _keyVaultHelper = keyVaultHelper;
            _httpClient = httpClient;
        }

        public async Task<WeightResponseObject> GetWeightLogs(string startDate, string endDate)
        {
            try
            {
                var fitbitAccessToken = await _keyVaultHelper.RetrieveSecretFromKeyVaultAsync(_configuration["AccessTokenName"]);
                _httpClient.DefaultRequestHeaders.Clear();
                Uri getMonthlyWeightLogs = new Uri($"https://api.fitbit.com/1/user/-/body/log/weight/date/{startDate}/{endDate}.json");
                var request = new HttpRequestMessage(HttpMethod.Get, getMonthlyWeightLogs);
                request.Content = new StringContent("");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fitbitAccessToken.Value);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                var weightResponse = JsonConvert.DeserializeObject<WeightResponseObject>(responseString);

                return weightResponse;
            }
            catch (Exception ex)
            {
                throw ex;
            }         
        }
    }
}
