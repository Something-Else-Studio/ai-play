
using Anthropic.SDK;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

using OpenAI;

using System.ClientModel;
using System.ComponentModel;


var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var openAIClient =
    new ChatClientBuilder()
    .UseFunctionInvocation()
    .Use(new OpenAIClient(config["OpenAIKey"] ?? throw new Exception("No open ai key")).AsChatClient(modelId: "gpt-4o-mini"));

var anthropicClient = new ChatClientBuilder()
    .UseChatOptions((o) =>
    {
        var options = o ?? new ChatOptions();
        options.ModelId = "claude-3-5-sonnet-20241022";
        options.MaxOutputTokens = 8192;
        return options;
    })
    .UseFunctionInvocation()
    .Use(new AnthropicClient(config["AnthropicKey"] ?? throw new Exception("No anthropic key")).Messages);



var grokAnthropicClient = new ChatClientBuilder()
    .UseChatOptions((o) =>
    {
        var options = o ?? new ChatOptions();
        options.ModelId = "grok-beta";
        options.MaxOutputTokens = 8192;
        return options;
    })
    .UseFunctionInvocation()
    .Use((new AnthropicClient(config["XaiKey"] ?? throw new Exception("No Xai key")) { ApiUrlFormat = "https://api.x.ai/{0}/{1}" }).Messages);

var grokOAIClient = new ChatClientBuilder()
    .UseChatOptions((o) =>
    {
        var options = o ?? new ChatOptions();
        //options.ModelId = "grok-beta";
        options.MaxOutputTokens = 8192;
        return options;
    })
    .UseFunctionInvocation()
    .Use(new OpenAIClient(new ApiKeyCredential(config["XaiKey"] ?? throw new Exception("No Xai key")), new OpenAIClientOptions { Endpoint = new Uri("https://api.x.ai/v1") }).AsChatClient(modelId: "grok-beta"));

var grokClient = grokOAIClient;

async Task RunRequest(string userRequest, ChatOptions? options = null)
{
    var responses = await Task.WhenAll(
     openAIClient.CompleteAsync(userRequest, options),
     anthropicClient.CompleteAsync(userRequest, options),
     grokClient.CompleteAsync(userRequest, options)
 );
    foreach (var response in responses)
    {
        Console.WriteLine(response.Message);
    }

}

[Description("Get the weather")]
string GetWeather()
{
    var r = new Random();
    return r.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
}

{

    var markdown = await File.ReadAllTextAsync("benefits.md");
    var userRequest = $"""
    Please summarise the following text in 20 words or less:

    <text>
    {markdown}
    </text>
    """;

    await RunRequest(userRequest);

}

{
    var userRequest = $"""
    Do I need an umbrella today?
    """;
    var options = new ChatOptions
    {
        Tools = [AIFunctionFactory.Create(GetWeather)],
        ToolMode = ChatToolMode.Auto
    };

    await RunRequest(userRequest, options);
}
