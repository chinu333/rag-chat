# rag-chat
This is a minimal implementation of RAG pattern using Semantic Kernel as a foundation for enabling enterprise data ingestion, and long-term memory. RAG is a pattern that uses pretrained LLM along with your own data to generate responses. The demo includes loading the data into QDrant (Vector DB) and used Semantic Kernel to orchestrate and generate responses from the vector DB.

## Configure your environment
Before you get started, make sure you have the following requirements in place:
- [Visual Studio Code](http://aka.ms/vscode) with extensions:
  - [C# Extension](https://aka.ms/csharp/vscode)
  - [Azure Functions Extension](https://aka.ms/azfn/vscode)
- [.NET 7.0 SDK](https://aka.ms/net70) for building and deploying .NET 7 projects.
- [Azure Function Core Tools 4.x](https://aka.ms/azfn/coretools) for managing Azure Functions
- [Azure OpenAI API key](https://learn.microsoft.com/en-us/answers/questions/1302744/openai-api-key-azure-ai-studio) for using the Azure OpenAI API (or click [here](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu) to signup).

Then, open a terminal and clone this repo with the following command:

```bash
git clone https://github.com/chinu333/rag-chat.git
```
## Create an Azure Function project
1. Open a new Visual Studio Code window and click on the Azure extension (or press `SHIFT+ALT+A`).
1. Mouse-over `WORKSPACE` (in the lower left pane) and select `Create Function` (i.e., +⚡) to create a new local Azure function project.
1. Select `Browse` and create a folder called `myfunc` inside the cloned repo's `src` directory to house your Azure Function code (e.g., `rag-chat/src/myfunc`). Then use the selections below when creating the project:

   | Selection       | Value                       |
   | ---------       | -----                       |
   | Language        | `C#`                        |
   | Runtime         | `.NET 7 Isolated`           |
   | Template        | `Http trigger`              |
   | Function name   | `MyChatFunction`            |
   | Namespace       | `My.MyChatFunction`         |
   | Access rights   | `Function`                  |

Now close and reopen Visual Studio Code, this time opening the `rag-chat` folder so you can view and interact with the entire repository.

## Add Semantic Kernel to your Azure Function
1. Open a terminal window, change to the directory with your Azure Function project file (e.g., `rag-chat/src/myfunc`), 
    and run the `dotnet` command below to add the Semantic Kernel NuGet package to your project.
    ```bash
    dotnet add package Microsoft.SemanticKernel --prerelease -v 0.14.547.1-preview
    ```

    In addition, use the commands below to configure .NET User Secrets and then securely store your OpenAI API key.
    ```bash
    dotnet add package Microsoft.Extensions.Configuration.UserSecrets
    dotnet user-secrets init --id semantic-kernel-rag-chat
    dotnet user-secrets set "AZURE_OPENAI_APIKEY" "<your Azure OpenAI API key>"
    ```

    > Make sure to specify `rag-chat` as the `--id` parameter. This will enable you to access your secrets from any of the projects in this repository.

    1. Back in your Azure Function project in Visual Studio Code, open the `Program.cs` and `MyChatFunction.cs` file and replace everything in the file with the content below.

1. The complete code files (with additional comments).
    <details>
    <summary>Program.cs</summary>

    ```csharp
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
            string openAiApiKey = ""; //configuration["OPENAI_APIKEY"];

            QdrantMemoryStore memoryStore = new QdrantMemoryStore(
                host: "http://localhost",
                port: 6333,
                vectorSize: 1536,
                logger: sp.GetRequiredService<ILogger<QdrantMemoryStore>>());

            AzureCognitiveSearchMemory memory = new AzureCognitiveSearchMemory(
                "ENDPOINT",
                "KEY"
            );


            IKernel kernel = new KernelBuilder()
                .WithLogger(sp.GetRequiredService<ILogger<IKernel>>())
                .Configure(config => config.AddAzureChatCompletionService(
                    deploymentName: "DEPLOYMENT NAME",
                    endpoint: "ENDPOINT",
                    apiKey: openAiApiKey))
                // .Configure(c => c.AddAzureTextEmbeddingGenerationService(
                //     deploymentName: "text-embedding-ada-002",
                //     endpoint: "ENDPOINT",
                //     apiKey: openAiApiKey))
                // .WithMemoryStorage(memoryStore)
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

    ```
    </details>

    <details>
    <summary>MyChatFunction.cs</summary>

    ```csharp
    using System.Net;
    using System.Text;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.SemanticKernel.AI.ChatCompletion;
    using Microsoft.SemanticKernel.Memory;
    using Microsoft.SemanticKernel;

    namespace My.MyChatFunction
    {
        public class MyChatFunction
        {
            private readonly ILogger _logger;
            private readonly IKernel _kernel;
            private readonly IChatCompletion _chat;
            private readonly ChatHistory _chatHistory;

            public MyChatFunction(ILoggerFactory loggerFactory, IKernel kernel, ChatHistory chatHistory, IChatCompletion chat)
            {
                _logger = loggerFactory.CreateLogger<MyChatFunction>();
                _kernel = kernel;
                _chat = chat;
                _chatHistory = chatHistory;
            }

            [Function("MyChatFunction")]
            public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
            {
                // Add the user's chat message to the history.
                // _chatHistory!.AddMessage(ChatHistory.AuthorRoles.User, await req.ReadAsStringAsync() ?? string.Empty);

                string message = await SearchMemoriesAsync(_kernel, await req.ReadAsStringAsync() ?? string.Empty);
                _chatHistory!.AddMessage(ChatHistory.AuthorRoles.User, message);

                // Send the chat history to the AI and receive a reply.
                string reply = await _chat.GenerateMessageAsync(_chatHistory, new ChatRequestSettings());

                // Add the AI's reply to the chat history for next time.
                _chatHistory.AddMessage(ChatHistory.AuthorRoles.Assistant, reply);

                // Send the AI's response back to the caller.
                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteString(reply);
                return response;
            }

            private async Task<string> SearchMemoriesAsync(IKernel kernel, string query)
            {
                StringBuilder result = new StringBuilder();
                result.Append("The below is relevant information.\n[START INFO]");
                
                // Search for memories that are similar to the user's input.
                const string memoryCollectionName = "ms10k";
                IAsyncEnumerable<MemoryQueryResult> queryResults = 
                    kernel.Memory.SearchAsync(memoryCollectionName, query, limit: 3, minRelevanceScore: 0.77);

                // For each memory found, try to get previous and next memories.
                await foreach (MemoryQueryResult r in queryResults)
                {
                    int id = int.Parse(r.Metadata.Id);
                    MemoryQueryResult? rb2 = await kernel.Memory.GetAsync(memoryCollectionName, (id - 2).ToString());
                    MemoryQueryResult? rb = await kernel.Memory.GetAsync(memoryCollectionName, (id - 1).ToString());
                    MemoryQueryResult? ra = await kernel.Memory.GetAsync(memoryCollectionName, (id + 1).ToString());
                    MemoryQueryResult? ra2 = await kernel.Memory.GetAsync(memoryCollectionName, (id + 2).ToString());

                    if (rb2 != null) result.Append("\n " + rb2.Metadata.Id + ": " + rb2.Metadata.Description + "\n");
                    if (rb != null) result.Append("\n " + rb.Metadata.Description + "\n");
                    if (r != null) result.Append("\n " + r.Metadata.Description + "\n");
                    if (ra != null) result.Append("\n " + ra.Metadata.Description + "\n");
                    if (ra2 != null) result.Append("\n " + ra2.Metadata.Id + ": " + ra2.Metadata.Description + "\n");
                }

                result.Append("\n[END INFO]");
                result.Append($"\n{query}");

                return result.ToString();
            }        
        }
    }
    ```
    </details>


## Configure Keys/Endpoints
Congigure keys/endpoints in following places:
- src/myfunc/Program.cs
- src/importmemories/Program.cs


## Run the function locally
1. Run your Azure Function locally by opening a terminal, changing directory to your Azure Function project (e.g., `rag-chat/src/myfunc`), and starting the function by running
    ```bash
    func start
    ```
    > Make note of the URL displayed (e.g., `http://localhost:7071/api/MyChatFunction`).

1. Start the test console application
   Open a second terminal and change directory to the `chatconsole` project folder (e.g., `rag-chat/src/chatconsole`) and run the application using the Azure Function URL.
   ```bash
   dotnet run http://localhost:7071/api/MyChatFunction
   ```
1. Type a message and press enter to verify that we are able to chat with the AI!
    ```
    Input: Hello, how are you?
    AI: Hello! As an AI language model, I don't have feelings, but I'm functioning properly and ready to 
    assist you. How can I help you today?
    ```
   
 1. Now let's try to ask about something that is not in the current AI model, such as "What was Microsoft's total revenue for 2022?"
    ```
    Input: What was Microsoft's cloud revenue for 2022?
    AI: I'm sorry, but I cannot provide information about Microsoft's cloud revenue for 2022 as it is not yet 
    available. Microsoft's fiscal year 2022 ends on June 30, 2022, and the company typically releases its 
    financial results a few weeks after the end of the fiscal year. However, Microsoft's cloud revenue for 
    fiscal year 2021 was $59.5 billion, an increase of 34% from the previous year.
    ```
    As you can see the AI is a bit out of date with its answers.

    Next we'll add a 'knowledge base' to the chat to help answer questions such as those above more accurately.


# Memories of Enterprise Data
Semantic Kernel's memory stores are used to integrate data from your knowledge base into AI interactions.
Any data can be added to a knowledge base and you have full control of that data and who it is shared with.
SK uses [embeddings](https://learn.microsoft.com/en-us/azure/ai-services/openai/tutorials/embeddings?tabs=command-line) to encode data and store it in a 
vector database. Using a vector database also allows us to use vector search engines to quickly find the most 
relevant data for a given query that we then share with the AI. In this chapter, we'll add a memory store to 
our chat function, import the Microsoft revenue data, and use it to answer the question from Chapter 1.

## Configure your environment
Before you get started, make sure you have the following additional requirements in place:
- [Docker Desktop](https://www.docker.com/products/docker-desktop) for hosting the [Qdrant](https://github.com/qdrant/qdrant) vector search engine.
   > Note that a different vector store, such as Pinecone or Weviate, could be leveraged.

## Deploy Qdrant VectorDB and Populate Data
In this section we deploy the Qdrant vector database locally and populate it with example data (i.e., Microsoft's 2022 10-K financial report). This will take approximately 15 minutes to import and will use OpenAI’s embedding generation service to create embeddings for the 10-K.

1. Start Docker Desktop and wait until it is running.

1. Open a terminal and use Docker to pull down the container image for Qdrant.
    ```bash
    docker pull qdrant/qdrant
    ```

1. Change directory to the root of this repo (e.g., `rag-chat`) and create a `./data/qdrant` directory for Qdrant to use as persistent storage.
   Then start the Qdrant container on port `6333` using the `./data/qdrant` folder as the persistent storage location.
    ```bash
    mkdir ./data/qdrant
    docker run --name mychat -p 6333:6333 -v "$(pwd)/data/qdrant:/qdrant/storage" qdrant/qdrant
    ```
    > To stop the container, in another terminal window run `docker container stop mychat; docker container rm mychat;`.

1. Open a second terminal and change directory to the `importmemories` project folder in this repo (e.g., `rag-chat/src/importmemories`). Run the `importmemories` tool with the command below to populate the vector database with your data.
    > Make sure the `--collection` argument matches the `collectionName` variable in the `SearchMemoriesAsync` method above.
    
    > **Note:** This may take several minutes to several hours depending on the size of your data. This repo contains 
      Microsoft's 2022 10-K financial report data as an example which should normally take about 15 minutes to import.
        
	```bash
    dotnet run -- --memory-type qdrant --memory-url http://localhost:6333 --collection ms10k --text-file ../../data/ms10k.txt
	```
    > When importing your own data, try to import all files at the same time using multiple `--text-file` arguments. 
    > This example leverages incremental indexes which are best constructed when all data is present. 
    
    > If you want to reset the memory store, delete and recreate the directory in step 2, or create a new directory to use.

## Run the function locally
1. With Qdrant running and populated, run your Azure Function locally by opening a terminal, changing directory to your Azure Function project (e.g., `rag-chat/src/myfunc`), and starting the function by running
    ```bash
    func start
    ```
    > Make a note of the URL displayed (e.g., `http://localhost:7071/api/MyChatFunction`).

1. Start the test console application
   Open a second terminal and change directory to the `chatconsole` project folder (e.g., `rag-chat/src/chatconsole`) and run the application using the Azure Function URL.
   ```bash
   dotnet run http://localhost:7071/api/MyChatFunction
   ```
1. Type a message and press enter to verify that we are able to chat with the AI!
    ```
    Input: Hello, how are you?
    AI: Hello! As an AI language model, I don't have feelings, but I'm functioning properly and ready to 
    assist you. How can I help you today?
    ```
   
 1. Now let's try ask the same question from before about Microsoft's 2022 revenue
    ```
    Input: What was Microsoft's cloud revenue for 2022?
    AI: Microsoft's cloud revenue for 2022 was $91.2 billion.
    ```
    > The AI now has the ability to search through the Microsoft 10-K financial report and find the answer to our question.
    > Let's try another...
    ```
    Input: Did linkedin's revenue grow in 2022?
    AI: Yes, LinkedIn's revenue grew in 2022. It increased by $3.5 billion or 34% driven by a strong job 
    market in the Talent Solutions business and advertising demand in the Marketing Solutions business.