using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
using LinkedInAutoPoster.Services;


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

            var url = "https://www.linkedin.com/oauth/v2/authorization" +
              $"?response_type=code" +
              $"&client_id={clientId}" +
              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
              $"&scope=openid%20profile%20w_member_social" +
              $"&state={state}";
            return Redirect(url);
        }
        [HttpPost("post")]
        public async Task<IActionResult> PostManually(string message,
        [FromServices] LinkedInPostService linkedin)
        {
            var result = await linkedin.Publish(message);
            return Ok(result);
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
    [ApiController]
    [Route("autopost")]
    public class AutoPostController : ControllerBase
    {
        private readonly OpenAiService _openAi;
        private readonly LinkedInPostService _linkedin;

        public AutoPostController(OpenAiService openAi, LinkedInPostService linkedin)
        {
            _openAi = openAi;
            _linkedin = linkedin;
        }

        [HttpPost("post-ai")]
        public async Task<IActionResult> PostUsingAI(string topic)
        {
            // 1️⃣ generate
            string text = await _openAi.GeneratePost(topic);

            // 2️⃣ publish
            var result = await _linkedin.Publish(text);

            return Ok(new { text, result });
        }
    }


}
