using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using Rust;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;

namespace Oxide.Plugins
{
    [Info("RustAI", "Sigilo", "1.0.0")]
    class RustAI : RustPlugin
    {
        private PluginConfig _config { get; set; }
        private Dictionary<string, float> _lastUsageTime = new Dictionary<string, float>();
        private float _lastGlobalUsageTime;

        private const string UsePermission = "rustai.use";
        private const string SwitchModelPermission = "rustai.switchmodel";

        public class PluginConfig
        {
            public string OpenAIApiURL { get; set; }
            public string TextGenerationApiUrl { get; set; }
            public string ActivationKeyword { get; set; }
            public float UserCooldownInSeconds { get; set; }
            public float GlobalCooldownInSeconds { get; set; }
            public string SystemPrompt { get; set; }
            public string ModelType { get; set; }
            public string OpenAI_API_Key { get; set; }
            public string ModelName { get; set; }
            public int MaxTokens { get; set; }
            public double Temperature { get; set; }
            public string Character { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    OpenAIApiURL = "https://api.openai.com/v1/chat/completions",
                    TextGenerationApiUrl = "http://0.0.0.0:5000/v1/chat/completions",
                    ActivationKeyword = "!ai",
                    UserCooldownInSeconds = 30.0f,
                    GlobalCooldownInSeconds = 10.0f,
                    SystemPrompt = "You are a helpful assistant.",
                    ModelType = "openai",
                    OpenAI_API_Key = "",
                    ModelName = "gpt-3.5-turbo",
                    MaxTokens = 100,
                    Temperature = 0.9,
                    Character = "Assistant"
                };
            }
        }

        public class Payload
        {
            public string prompt { get; set; }
            public int max_tokens { get; set; }
            public double temperature { get; set; }
        }

        public class Response
        {
            public string id { get; set; }
            public string created { get; set; }
            public string model { get; set; }
            public Choice[] choices { get; set; }
        }

        public class Choice
        {
            public Message message { get; set; }
            public string text { get; set; }
        }

        public class Message
        {
            public string content { get; set; }
        }

        public class ServerMessage
        {
            public string @event { get; set; }
            public string text { get; set; }
        }

        private void Init()
        {
            if (permission != null)
            {
                permission.RegisterPermission(UsePermission, this);
                permission.RegisterPermission(SwitchModelPermission, this);
            }

            _config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(PluginConfig.DefaultConfig(), true);
        }

        private bool HasPermission(BasePlayer player)
        {
            bool hasPermission = permission.UserHasPermission(player.UserIDString, UsePermission);
            return hasPermission;
        }

        private void OnPlayerChat(BasePlayer player, string message)
        {
            if (message.StartsWith(_config.ActivationKeyword) && HasPermission(player))
            {
                if (!HasCooldownElapsed(player))
                {
                    return;
                }

                string prompt = System.Net.WebUtility.UrlEncode(message.Substring(_config.ActivationKeyword.Length).Trim());

                GenerateTextAsync(player, prompt);
            }
        }

        private bool HasCooldownElapsed(BasePlayer player)
        {
            float lastUsageTime;

            if (_lastUsageTime.TryGetValue(player.UserIDString, out lastUsageTime))
            {
                float userElapsedTime = Time.realtimeSinceStartup - lastUsageTime;
                float globalElapsedTime = Time.realtimeSinceStartup - _lastGlobalUsageTime;

                if (userElapsedTime < _config.UserCooldownInSeconds)
                {
                    int timeLeft = Mathf.FloorToInt(_config.UserCooldownInSeconds - userElapsedTime);
                    player.ChatMessage($"You must wait <color=green>{timeLeft}</color> seconds before using the command again.");
                    return false;
                }

                if (globalElapsedTime < _config.GlobalCooldownInSeconds)
                {
                    int timeLeft = Mathf.FloorToInt(_config.GlobalCooldownInSeconds - globalElapsedTime);
                    player.ChatMessage($"Everyone must wait <color=green>{timeLeft}</color> seconds before using the command again.");
                    return false;
                }
            }

            _lastUsageTime[player.UserIDString] = Time.realtimeSinceStartup;
            _lastGlobalUsageTime = Time.realtimeSinceStartup;
            return true;
        }

        public async void GenerateTextAsync(BasePlayer player, string prompt)
        {
            string apiUrl = _config.ModelType == "openai" ? _config.OpenAIApiURL : _config.TextGenerationApiUrl;

            if (_config.ModelType == "openai")
            {
                var payload = new
                {
                    messages = new[]
                    {
                        new {role = "system", content = _config.SystemPrompt},
                        new {role = "user", content = string.IsNullOrEmpty(prompt) ? "Hello" : prompt}
                    },
                    model = _config.ModelName,
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var request = new UnityWebRequest(apiUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + _config.OpenAI_API_Key);

                var asyncOp = request.SendWebRequest();

                var tcs = new TaskCompletionSource<bool>();
                asyncOp.completed += _ => tcs.SetResult(true);
                await tcs.Task;

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(request.error);
                }
                else
                {
                    var responseContent = request.downloadHandler.text;
                    var responseObject = JsonConvert.DeserializeObject<Response>(responseContent);

                    if (responseObject.choices != null && responseObject.choices.Length > 0)
                    {
                        string chatMessage = responseObject.choices[0].message.content;
                        player.ChatMessage(chatMessage);
                    }
                }
            }
            else if (_config.ModelType == "textgeneration")
            {
                Puts("Text generation started");

                if (string.IsNullOrEmpty(prompt))
                {
                    prompt = "Hello";
                }
                else
                {
                    prompt = System.Net.WebUtility.UrlDecode(prompt);
                }

                string messageContent = _config.SystemPrompt + " " + prompt;

                var payload = new
                {
                    messages = new[]
                    {
                        new {role = "user", content = messageContent}
                    },
                    mode = "chat",
                    character = _config.Character,
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature
                };

                var jsonPayload = JsonConvert.SerializeObject(payload);
                var request = new UnityWebRequest(apiUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + _config.OpenAI_API_Key);

                var asyncOp = request.SendWebRequest();

                var tcs = new TaskCompletionSource<bool>();
                asyncOp.completed += _ => tcs.SetResult(true);
                await tcs.Task;

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(request.error);
                }
                else
                {
                    var responseContent = request.downloadHandler.text;
                    var responseObject = JsonConvert.DeserializeObject<Response>(responseContent);

                    if (responseObject.choices != null && responseObject.choices.Length > 0)
                    {
                        string chatMessage = responseObject.choices[0].message.content;
                        player.ChatMessage(chatMessage);
                    }
                }
            }
        }

        [ChatCommand("switchmodel")]
        private void SwitchModelCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, SwitchModelPermission))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            if (_config.ModelType == "openai")
            {
                _config.ModelType = "textgeneration";
                player.ChatMessage("Switched to text generation model.");
            }
            else
            {
                _config.ModelType = "openai";
                player.ChatMessage("Switched to OpenAI model.");
            }

            Config.WriteObject(_config, true);
        }
    }
}
