# rag-chat
The repo is to implement the RAG pattern. RAG is a pattern that uses pretrained LLM along with your own data to generate responses. The demo includes loading the data into QDrant (Vector DB) and used Semantic Kernel to orchestrate and generate responses from the vector DB.

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

