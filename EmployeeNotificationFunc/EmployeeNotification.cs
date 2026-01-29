using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzLearning.Func.Emp.Functions
{
    public class EmployeeNotification
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmployeeNotification(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<EmployeeNotification>();
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        [Function("NotifyEmployees")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("NotifyEmployees function triggered.");

            // 1️⃣ Read Logic App URL from App Settings
            var logicAppUrl = _configuration["LogicApp:EmployeeCreatedUrl"];
            if (string.IsNullOrEmpty(logicAppUrl))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("LogicApp URL is not configured.");
                return bad;
            }

            // 2️⃣ Read incoming request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Request body is empty.");
                return bad;
            }

            _logger.LogInformation($"Incoming payload: {requestBody}");

            try
            {
                // 3️⃣ Forward SAME payload to Logic App
                var content = new StringContent(
                    requestBody,
                    Encoding.UTF8,
                    "application/json");

                var logicResponse = await _httpClient.PostAsync(logicAppUrl, content);

                var response = req.CreateResponse(
                    logicResponse.IsSuccessStatusCode
                        ? HttpStatusCode.OK
                        : HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    $"Logic App called. Status: {logicResponse.StatusCode}");

                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error calling Logic App.");

                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("Failed to call Logic App.");
                return error;
            }
        }
    }
}
