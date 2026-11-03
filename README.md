# .NET AI Banking Assistant

A console chatbot built with **C# / .NET**, **Microsoft.Extensions.AI** and a local LLM (**Ollama, qwen2.5:3b**).

## Features
- Conversation memory (chat history management)
- Tool calling: the AI calls C# methods for application status and EMI calculation
- Input validation and guardrails on AI-generated tool arguments
- Provider-agnostic: uses `IChatClient`, so it can switch to Claude / Azure OpenAI

## Run
1. Install Ollama → `ollama pull qwen2.5:3b`
2. `dotnet run`

## Sample
You: What is the status of APP101?
   [Tool called: GetApplicationStatus(APP101)]
AI: Your application APP101 is Approved - credit limit ₹1,50,000.

## Lessons learned
- Small models (llama3.2 3B) called tools unnecessarily and copied example IDs from the prompt;
  fixed with stricter tool descriptions and a model with better tool-calling ability.
- LLMs must not do financial math; calculations run in C#, and the AI only extracts the inputs.