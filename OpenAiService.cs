using OpenAI;
using OpenAI.Chat;

namespace LinkedInAutoPoster.Services
{
    public class OpenAiService
    {
        private readonly ChatClient _client;

        public OpenAiService(IConfiguration config)
        {
            _client =  new(model: "gpt-4.1-mini", apiKey: config["OpenAI:ApiKey"]);
        }

        public async Task<string> GeneratePost(string topic)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You write short human LinkedIn posts. No hashtags."),
                new UserChatMessage($"Topic: {topic}")
            };

           //  _client = new(model: "gpt-4.1-mini", apiKey: "your-api-key");

            // Your exact replacement
            ChatCompletion response = await _client.CompleteChatAsync(messages);
            return response?.Content?[0]?.Text ?? string.Empty;

            ;
        }
    }
}
