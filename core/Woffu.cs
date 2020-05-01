using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace selfish
{
    public class Woffu
    {
        public async Task<bool> TryToSignTodayAsync(string user, string password)
        {
            var httpClient = await GetClientAsync(user, password);
            var userId = await GetUserIdAsync(httpClient);
            var times = await GetTimesToSignAsync(httpClient, userId);

            if (times == null || times.TrueEndTime.HasValue) return false;

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

            return Time.Create(today);
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
            var result = await httpClient.PostAsync("https://app.woffu.com/api/signs", new StringContent(json, Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode;
        }

    }
}
