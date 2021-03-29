using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace core
{
    public record WoffuOptions(string User, string Password);

    public class Woffu
    {
        private readonly ILogger<Woffu> _logger;
        private readonly WoffuOptions _woffuOptions;

        public Woffu(ILogger<Woffu> logger, WoffuOptions woffuOptions)
        {
            _logger = logger;
            _woffuOptions = woffuOptions;
        }

        public async Task<bool> TryToSignTodayAsync()
        {
            _logger.LogInformation($"Trying to sign today as user: {_woffuOptions.User}");

            var httpClient = await GetClientAsync(_woffuOptions.User, _woffuOptions.Password);
            var userId = await GetUserIdAsync(httpClient);

            _logger.LogInformation($"The userId is : {userId} for user: {_woffuOptions.User}");

            var times = await GetTimesToSignAsync(httpClient, userId);

            if (times == null || times.TrueEndTime.HasValue) return false;

            _logger.LogInformation($"TrueStartTime: {times.TrueStartTime} TrueEndTime: {times.TrueEndTime}");

            return await DoSignAsync(httpClient, userId, times);
        }

        async Task<HttpClient> GetClientAsync(string user, string password)
        {
            var content = new StringContent($"grant_type=password&username={user}&password={password}");
            var httpClient = new HttpClient();

            var result = await httpClient.PostAsync("https://app.woffu.com/token", content);
            using var stream = await result.Content.ReadAsStreamAsync();
            var response = await JsonSerializer.DeserializeAsync<JsonElement>(stream);

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer { response.GetProperty("access_token").GetString()}");

            return httpClient;
        }

        async Task<int> GetUserIdAsync(HttpClient httpClient)
        {
            var result = await httpClient.GetAsync($"https://app.woffu.com/api/users/");
            using var stream = await result.Content.ReadAsStreamAsync();
            var response = await JsonSerializer.DeserializeAsync<JsonElement>(stream);

            return response.GetProperty("UserId").GetInt32();
        }

        async Task<Time> GetTimesToSignAsync(HttpClient httpClient, int userId)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var result = await httpClient.GetAsync($"https://app.woffu.com/api/users/{userId}/diaries/presence?fromDate={date}&pageIndex=0&pageSize=1&toDate={date}");
            using var stream = await result.Content.ReadAsStreamAsync();
            var response = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
            var diaries = response.GetProperty("Diaries");

            if (diaries.ValueKind != JsonValueKind.Array || diaries.GetArrayLength() != 1) throw new Exception();

            var today = diaries[0];
            if (today.GetProperty("IsHoliday").GetBoolean() || today.GetProperty("IsWeekend").GetBoolean()) return null;

            var startTime = today.GetProperty("StartTime").GetString() ?? throw new InvalidOperationException("StartTime");
            var endTime = today.GetProperty("EndTime").GetString() ?? throw new InvalidOperationException("EndTime");
            var trueStartTime = today.GetProperty("TrueStartTime").GetString();
            var trueEndTime = today.GetProperty("TrueEndTime").GetString();

            return Time.Create(startTime, endTime, trueStartTime, trueEndTime);
        }

        async Task<bool> DoSignAsync(HttpClient httpClient, int userId, Time times)
        {
            var json = $@"
{{
    ""UserId"": ""{userId}"",
    ""Date"": ""{times.GetDateToSign()}"",
    ""TimezoneOffset"": {TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Hours}
}}
";
            _logger.LogInformation($"Trying to sign with data: {json}");

            var result = await httpClient.PostAsync("https://app.woffu.com/api/signs", new StringContent(json, Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode;
        }

    }
}
