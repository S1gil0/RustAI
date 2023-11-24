# RustAI
The RustAI plugin integrates AI generative models into Rust servers, supporting both Text Generation WebUI for custom local models and OpenAI models, simultaneously.
Admins can switch between models using a command or via the configuration file. The plugin includes per-user and global cooldowns to prevent spam and/or overwhelming the model
 

Installation Instructions:

Place the "RustAI.cs" file in the Oxide plugins directory
Edit the config file at oxide/config/RustAI.json
Reload the plugin with o.reload RustAI
 

Configuration:

Open the server's configuration file at oxide/config/RustAI.json.

Adjust settings such as API URLs, activation keyword, cooldowns, and model types.

To use with OpenAI, just enter your OpenAI API Key.
Al base configurations and urls are pre entered

To use with Text Generation WebUI, first you need a local model running with the api extension.
Then enter the IP in TextGenerationApiUrl field. Ports and urls are already entered with the default values, it just needs the ip of the machine running the model.
Irrelevant fields for this model, like openai api key and modelname will be ignored.
 

Permissions:

Grant the "rustai.use" permission to players for AI interaction.

Grant the "rustai.switchmodel" permission to admins for model switching.
 

Activating the AI:

Players with "rustai.use" permission trigger the AI by starting messages with the activation keyword (default:: !ai).

The AI model responds with generated text based on the provided prompt.
 

Switching AI Models:

Users with "rustai.switchmodel" permission use "/switchmodel" to toggle between Text Generation WebUI and OpenAI models.

Users receive confirmation messages and the plugin automatically saves the updated configuration.

Feel free to try it, comment and suggest ideas for it.


Example configuration:
 

{
  "OpenAIApiURL": "https://api.openai.com/v1/chat/completions",
  "TextGenerationApiUrl": "http://144.144.144.144:5000/v1/completions",
  "ActivationKeyword": "!ai",
  "UserCooldownInSeconds": 30.0,
  "GlobalCooldownInSeconds": 10.0,
  "SystemPrompt": "You are a rust server assistant.",
  "ModelType": "openai",
  "OpenAI_API_Key": "OpenAI API Key Here",
  "ModelName": "gpt-3.5-turbo",
  "MaxTokens": 100,
  "Temperature": 1.2
}
 
