using GithubStatApi.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GithubStatApi
{
    public class GithubFunctions
    {
        private HttpClient _httpClient = new HttpClient();

        [FunctionName("GetGithubData")]
        public IActionResult GetGithubData(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log,
            [Blob("github-data/current", FileAccess.Read, Connection = "AzureBlobStorageConnectionString")] Stream blobStream)
        {
            StreamReader reader = new StreamReader(blobStream);
            string content = reader.ReadToEnd();

            return new OkObjectResult(content);
        }

        [FunctionName("UpdateGithubData")]
        public async Task UpdateGithubData([TimerTrigger("0 0 0 * * *", RunOnStartup = false)] TimerInfo myTimer, ILogger log,
            [Blob("github-data/current", FileAccess.Write, Connection = "AzureBlobStorageConnectionString")] Stream blobStream)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PostmanRuntime/7.32.3");

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/users/WallochMatt/repos?per_page=100&");
            request.Headers.Host = "api.github.com";

            using HttpResponseMessage allDataResponse = await _httpClient.SendAsync(request);

            var rawContent = await allDataResponse.Content.ReadAsStringAsync();

            var allData = JsonConvert.DeserializeObject<GithubDataDtoList>($"{{\"data\":{rawContent}}}");

            var repoDatas = await Task.WhenAll(
                allData.data
                .Select(async x => {
                    using (HttpResponseMessage lanuagesResponse = await _httpClient.GetAsync(x.languages_url))
                    {
                        var languagesJson = await lanuagesResponse.Content.ReadAsStringAsync();

                        return new RepoData()
                        {
                            name = x.name,
                            html_url = x.html_url,
                            _private = x._private ?? false,
                            description = x.description, // getting the readme would probably be better
                            allLanguages = languagesJson
                        };
                    }
                })
            );

            var strData = JsonConvert.SerializeObject(repoDatas);
            StreamWriter writer = new StreamWriter(blobStream);
            writer.Write(strData);
            writer.Flush();
        }
    }
}
