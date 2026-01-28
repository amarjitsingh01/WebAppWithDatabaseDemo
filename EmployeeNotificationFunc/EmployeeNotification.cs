using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Function triggered to call Logic App.");

            var logicAppUrl = _configuration["LogicApp:EmployeeCreatedUrl"];
            if (string.IsNullOrEmpty(logicAppUrl))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("LogicApp URL not configured.");
                return badResponse;
            }

            try
            {
                // Prepare payload (customize as needed)
                var payload = new { Message = "Triggered from Azure Function" };
                var json = JsonSerializer.Serialize(payload);

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var responseFromLogicApp = await _httpClient.PostAsync(logicAppUrl, content);

                var response = req.CreateResponse(responseFromLogicApp.IsSuccessStatusCode ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
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