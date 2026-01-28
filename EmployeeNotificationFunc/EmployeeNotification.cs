using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace AzLearning.Func.Emp.Functions
{
    public class EmployeeNotification
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public EmployeeNotification(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<EmployeeNotification>();
            _httpClient = new HttpClient();
            _configuration = configuration;
        }

        [Function("NotifyEmployees")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequestData req)
        {
            _logger.LogInformation("Function triggered to call Logic App.");

            // Get Logic App URL and Key from App Settings
            var logicAppUrl = _configuration["NotifyEmployeesUrl"];
            var logicAppKey = _configuration["NotifyEmployeesKey"]; // optional if included in URL

            if (string.IsNullOrEmpty(logicAppUrl))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Logic App URL not configured.");
                return badResponse;
            }

            try
            {
                // Read request body from WebApp
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Request body is empty.");
                    return badResponse;
                }

                // Forward the payload to Logic App
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // If you need the function key, you can append it as query param
                if (!string.IsNullOrEmpty(logicAppKey))
                {
                    logicAppUrl += $"?code={logicAppKey}";
                }

                var responseFromLogicApp = await _httpClient.PostAsync(logicAppUrl, content);

                var response = req.CreateResponse(
                    responseFromLogicApp.IsSuccessStatusCode ? HttpStatusCode.OK : HttpStatusCode.BadRequest
                );

                await response.WriteStringAsync($"Logic App triggered. Status code: {responseFromLogicApp.StatusCode}");
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error calling Logic App.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error calling Logic App.");
                return errorResponse;
            }
        }
    }
}
