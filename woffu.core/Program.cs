using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

            await woffu.LoginAsync(user, password);
            
        }
    }

    public class Woffu
    {
        public async Task LoginAsync(string user, string password)
        {
            var client = await GetClient(user, password);
            await SignAsync(client, null, null);

         
        }

        async Task<HttpClient> GetClient(string user, string password)
        {
            var content = new StringContent($"grant_type=password&username={user}&password={password}");
            var httpClient = new HttpClient();

            var result = await httpClient.PostAsync("https://app.woffu.com/token", content);
            using var stream = await result.Content.ReadAsStreamAsync();
            var response = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(stream);
            var jwtEncodedString = response["access_token"];

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtEncodedString}");

            return httpClient;
        }

        async Task SignAsync(HttpClient httpClient, string userId, string companyId)
        {
            var result = await httpClient.GetAsync($"https://app.woffu.com/api/users/{userId}/diaries/presence?fromDate=2020-05-01&pageIndex=0&pageSize=1&toDate=2020-05-01");
            using var stream = await result.Content.ReadAsStreamAsync();
            var response = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
            var diaries = response.GetProperty("Diaries");

            if (diaries.ValueKind != JsonValueKind.Array || diaries.GetArrayLength() != 1)
            {
                throw new Exception();
            }

            var diary = diaries[0];
            if (diary.GetProperty("isHoliday").GetBoolean() || diary.GetProperty("isWeekend").GetBoolean())
            {
                return;
            }

            var startTime = diary.GetProperty("StartTime");
            var endTime = diary.GetProperty("EndTime");
        }



        async Task Sign(HttpClient httpClient, string userId)
        {
            var result = await httpClient.GetAsync("https://app.woffu.com/api/signs");
            using var stream = await result.Content.ReadAsStreamAsync();
            var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(stream);

            var length = jsonElement.GetArrayLength();
            if (length > 1)
            {
                throw new Exception("Cannot sign more in/outs");
            }
            else if (length == 0)
            {
                //in
                return;
            }

            //out
            var date = jsonElement[0].GetProperty("Date").GetDateTime();

            
        }
    }
}
