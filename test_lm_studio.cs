#!/usr/bin/env dotnet-script
// dotnet-script test_lm_studio.cs

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class LMStudioTest
{
    static async Task Main(string[] args)
    {
        string apiUrl = args.Length > 0 ? args[0] : "http://localhost:1234/v1";

        Console.WriteLine($"Testing LM Studio API at: {apiUrl}");
        Console.WriteLine("================================================\n");

        using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
        {
            try
            {
                // Test 1: Check if API is reachable
                Console.WriteLine("[1] Checking if LM Studio is reachable...");
                var modelsResponse = await client.GetAsync($"{apiUrl}/models");
                if (!modelsResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ FAILED: Got HTTP {modelsResponse.StatusCode}");
                    Console.WriteLine($"   Make sure LM Studio is running at {apiUrl}");
                    return;
                }
                Console.WriteLine("✅ LM Studio is reachable\n");

                // Test 2: Check available models
                Console.WriteLine("[2] Checking available models...");
                var modelsBody = await modelsResponse.Content.ReadAsStringAsync();
                var modelsJson = JObject.Parse(modelsBody);
                var models = modelsJson["data"];

                if (models == null || models.Count() == 0)
                {
                    Console.WriteLine("❌ No models loaded");
                    Console.WriteLine($"   Response: {modelsBody}");
                    return;
                }

                foreach (var model in models)
                {
                    Console.WriteLine($"   📦 {model["id"]}");
                }
                Console.WriteLine();

                // Test 3: Send a test chat completion
                Console.WriteLine("[3] Testing chat completion endpoint...");
                var testPrompt = new
                {
                    model = "gemma-3",
                    messages = new[] {
                        new { role = "user", content = "You are a Greydwarf. A player just hit you. Write a one-sentence toxic insult." }
                    },
                    temperature = 0.9,
                    max_tokens = 50,
                    top_p = 0.95
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(testPrompt),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var chatResponse = await client.PostAsync($"{apiUrl}/chat/completions", content);

                if (!chatResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ FAILED: Got HTTP {chatResponse.StatusCode}");
                    var errorBody = await chatResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"   Error: {errorBody}");
                    return;
                }

                var chatBody = await chatResponse.Content.ReadAsStringAsync();
                var chatJson = JObject.Parse(chatBody);
                var insult = chatJson["choices"]?[0]?["message"]?["content"]?.Value<string>();

                if (string.IsNullOrWhiteSpace(insult))
                {
                    Console.WriteLine("❌ Got empty response from model");
                    Console.WriteLine($"   Response: {chatBody}");
                    return;
                }

                Console.WriteLine("✅ Chat completion works!\n");
                Console.WriteLine("[TEST RESULT]");
                Console.WriteLine("================================================");
                Console.WriteLine($"Generated insult: \"{insult}\"");
                Console.WriteLine("================================================");
                Console.WriteLine("\n✅ ALL TESTS PASSED - API is ready for the mod!");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");
                Console.WriteLine($"   Make sure LM Studio is running at {apiUrl}");
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"❌ Request timed out: {ex.Message}");
                Console.WriteLine($"   LM Studio may be slow or unresponsive");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex}");
            }
        }
    }
}
