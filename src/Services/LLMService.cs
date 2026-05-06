using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RagebateMobs.Services
{
    public class LLMService
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly string _apiUrl;
        private readonly string _model;
        private readonly ManualLogSource _logger;

        public LLMService(string apiUrl, string model, ManualLogSource logger)
        {
            _apiUrl = apiUrl.TrimEnd('/');
            _model = model;
            _logger = logger;
        }

        public async Task<string> GenerateInsultAsync(string prompt)
        {
            try
            {
                var payload = new
                {
                    model = _model,
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = 0.95,
                    max_tokens = 50,
                    top_p = 0.95
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await HttpClient.PostAsync($"{_apiUrl}/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"[Ragebait] LM Studio returned {response.StatusCode}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseBody);
                var insult = json["choices"]?[0]?["message"]?["content"]?.Value<string>();

                if (string.IsNullOrWhiteSpace(insult))
                {
                    _logger.LogWarning("[Ragebait] Empty response from LM Studio");
                    return null;
                }

                return insult.Trim();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"[Ragebait] Failed to connect to LM Studio: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"[Ragebait] LM Studio request timed out: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"[Ragebait] Failed to parse LM Studio response: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ragebait] Unexpected error calling LM Studio: {ex}");
                return null;
            }
        }
    }
}
