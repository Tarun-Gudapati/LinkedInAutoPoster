using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
namespace LinkedInAutoPoster.Services
{
    public class LinkedInPostService
    {
        private readonly IConfiguration _config;
        public LinkedInPostService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<(string status, string response)> Publish(string message)
        {
            var accessToken = _config["LinkedIn:AccessToken"];

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            http.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");

            // GET user id
            var meResponse = await http.GetAsync("https://api.linkedin.com/v2/userinfo");
            var meJson = await meResponse.Content.ReadAsStringAsync();
            dynamic me = JsonConvert.DeserializeObject(meJson);
            string sub = me.sub;
            string author = $"urn:li:person:{sub}";

            // Build payload
            var payload = new Dictionary<string, object>
            {
                ["author"] = author,
                ["lifecycleState"] = "PUBLISHED",
                ["specificContent"] = new Dictionary<string, object>
                {
                    ["com.linkedin.ugc.ShareContent"] = new Dictionary<string, object>
                    {
                        ["shareCommentary"] = new Dictionary<string, string>
                        {
                            ["text"] = message
                        },
                        ["shareMediaCategory"] = "NONE"
                    }
                },
                ["visibility"] = new Dictionary<string, string>
                {
                    ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("https://api.linkedin.com/v2/ugcPosts", content);

            return (response.StatusCode.ToString(), await response.Content.ReadAsStringAsync());
        }
    }
}
