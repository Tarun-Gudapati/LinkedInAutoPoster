using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;


namespace LinkedInAutoPoster.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _client;

        public AuthController(IConfiguration config)
        {
            _config = config;
            _client = new HttpClient();
        }


        [HttpGet("login")]
        public IActionResult Login()
        {
            var clientId = _config["LinkedIn:ClientId"];
            var redirectUri = _config["LinkedIn:RedirectUri"];
            var state = Guid.NewGuid().ToString("N");

            //var url =
            //"https://www.linkedin.com/oauth/v2/authorization" +
            //$"?response_type=code" +
            //$"&client_id={clientId}" +
            //$"&redirect_uri={redirectUri}" +
            //$"&scope=r_liteprofile%20w_member_social" +
            //$"&state={state}";

            var url = "https://www.linkedin.com/oauth/v2/authorization" +
              $"?response_type=code" +
              $"&client_id={clientId}" +
              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
              $"&scope=openid%20profile%20w_member_social" +
              $"&state={state}";
            return Redirect(url);
        }
        [HttpPost("post")]
        public async Task<IActionResult> PostToLinkedIn(string message)
        {
            var accessToken = "AQWzuf2HVVfaZEbDRX9v4vkV8P8MyinPa63jeLhuYlh_IcJxFxh_fjBT87yhOcw2b1JTNWIUFr2EynddPjledl_b7-q_8w10QJr9clUAaV6rFprkCoIcx5cWvZNt6MrpVb8IIqwcvhzcuHb1sEYlR5HtEOIl2mW8mLxeZiQ5nBGatgesHeKGsryyr17yKz8rnn3nahKGAWkLilYwzTOW2LH8kA1idznEci0Q__bhCGt4i5fTHekPe7ycXBf1wy6hD_1xxEi62dpod4ANklQ6Tl4T899J5QbebF7o-svOJmTvgll7QevdNfNBt-t2IDI8gz-uf1n94SfbP7bfaqMl6iMc7WPf8Q";

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            http.DefaultRequestHeaders.Add("X-Restli-Protocol-Version", "2.0.0");

            // 1️⃣ GET USER INFO (OPENID)
            var meResponse = await http.GetAsync("https://api.linkedin.com/v2/userinfo");
            var meJson = await meResponse.Content.ReadAsStringAsync();
            dynamic me = Newtonsoft.Json.JsonConvert.DeserializeObject(meJson);
            string sub = me.sub;   // IMPORTANT: sub NOT id
            string author = $"urn:li:person:{sub}";

            // 2️⃣ EXACT PAYLOAD
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

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 3️⃣ POST TO LINKEDIN
            var response = await http.PostAsync("https://api.linkedin.com/v2/ugcPosts", content);

            var result = await response.Content.ReadAsStringAsync();
            return Ok(new { status = response.StatusCode.ToString(), response = result, sentJson = json });
        }





        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code)
        {
            var tokenUrl = "https://www.linkedin.com/oauth/v2/accessToken";

            var clientId = _config["LinkedIn:ClientId"];
            var clientSecret = _config["LinkedIn:ClientSecret"];
            var redirectUri = _config["LinkedIn:RedirectUri"];

            var values = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            var response = await _client.PostAsync(tokenUrl, new FormUrlEncodedContent(values));
            var responseString = await response.Content.ReadAsStringAsync();

            return Ok(responseString);
        }
    }
}
