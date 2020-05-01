using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace woffu.core
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var woffu = new Woffu();

            var conf = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            var user = conf.GetValue<string>("User");
            var password = conf.GetValue<string>("Password");

            await woffu.TryToSignAsync(user, password);
            
        }
    }

    public class Woffu
    {
        public async Task TryToSignAsync(string user, string password)
        {
            var httpClient = await GetClientAsync(user, password);
            var userId = await GetUserIdAsync(httpClient);
            var times = await GetTimesToSignAsync(httpClient, userId);
            if (times == null || times.TrueEndTime.HasValue)
            {
                return;
            }
            
            await DoSignAsync(httpClient, times);

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

            if (diaries.ValueKind != JsonValueKind.Array || diaries.GetArrayLength() != 1)
            {
                throw new Exception();
            }

            var diary = diaries[0];
            if (diary.GetProperty("IsHoliday").GetBoolean() || diary.GetProperty("IsWeekend").GetBoolean())
            {
                return null;
            }

            return Time.Create(diary);
        }

        async Task DoSignAsync(HttpClient httpClient, Time times)
        {

            if (!times.TrueStartTime.HasValue)
            {

            }

            var dictionary = new Dictionary<string, object>
            {
                { "UserId", 1 },
                { "Date", 1 },
                { "TimezoneOffset", TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Hours }
            };


        }
    }

    public class Time
    {
        public int StartTime { get; private set; }
        public int EndTime { get; private set; }
        public int? TrueStartTime { get; private set; }
        public int? TrueEndTime { get; private set; }

        //no bugs checking :)
        public static Time Create(JsonElement element)
        {
            static int? GetHour(JsonElement hour) => 
                hour.ValueKind == JsonValueKind.Null ? (int?)null : DateTime.ParseExact(hour.GetString(), "HH:mm:ss", CultureInfo.InvariantCulture).Hour;

            return new Time
            {
                StartTime = GetHour(element.GetProperty("StartTime")).Value,
                EndTime = GetHour(element.GetProperty("EndTime")).Value,
                TrueStartTime = GetHour(element.GetProperty("TrueStartTime")),
                TrueEndTime = GetHour(element.GetProperty("TrueEndTime"))
            };
        }

        DateTime GetDateToSign()
        {

        }
    }
}
