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

