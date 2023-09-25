using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Connectors.Memory.AzureCognitiveSearch;

var hostBuilder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults();

hostBuilder.ConfigureAppConfiguration((context, config) =>
{
    config.AddUserSecrets<Program>();
});

hostBuilder.ConfigureServices(services =>
{
    services.AddSingleton<IKernel>(sp =>
    {
        // Retrieve the OpenAI API key from the configuration.
        IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
        string openAiApiKey = "ee368e26186644eea636b27c564b5c08"; //configuration["OPENAI_APIKEY"];

        QdrantMemoryStore memoryStore = new QdrantMemoryStore(
            host: "http://localhost",
            port: 6333,
            vectorSize: 1536,
            logger: sp.GetRequiredService<ILogger<QdrantMemoryStore>>());

        AzureCognitiveSearchMemory memory = new AzureCognitiveSearchMemory(
            "https://openaicogsearchmtc.search.windows.net",
            "r1BhoGzrXf0i1khp4QvZRG3blGTU8emp9NeJxjiwtQAzSeCcBgTr"
        );

        AzureCognitiveSearchMemory csmemory = new AzureCognitiveSearchMemory(
            "https://cs-mtcatl.search.windows.net",
            "QtfU62WeY5rx27SIY46adertuAqxiTgkAy0VzqzqC5AzSeDd4Yfb"
        );


        IKernel kernel = new KernelBuilder()
            .WithLogger(sp.GetRequiredService<ILogger<IKernel>>())
            .Configure(config => config.AddAzureChatCompletionService(
                deploymentName: "FurhatChat",
                endpoint: "https://openaimtcdemoinstance.openai.azure.com/",
                apiKey: openAiApiKey))
            .Configure(c => c.AddAzureTextEmbeddingGenerationService(
                deploymentName: "text-embedding-ada-002",
                endpoint: "https://openaimtcdemoinstance.openai.azure.com/",
                apiKey: openAiApiKey))
            .WithMemoryStorage(memoryStore)
            // .WithMemory(memory)
            .Build();

        return kernel;
    });

    // Provide a chat completion service client to our function.
    services.AddSingleton<IChatCompletion>(sp =>
        sp.GetRequiredService<IKernel>().GetService<IChatCompletion>());

    // Provide a persistant in-memory chat history store with the 
    // initial ChatGPT system message.
    const string instructions = "You are a helpful friendly assistant.";
    services.AddSingleton<ChatHistory>(sp =>
        sp.GetRequiredService<IChatCompletion>().CreateNewChat(instructions));
});

hostBuilder.Build().Run();
