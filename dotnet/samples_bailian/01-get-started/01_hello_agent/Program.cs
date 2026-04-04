// Copyright (c) Microsoft. All rights reserved.

// 本示例演示如何使用阿里百炼（DashScope）作为后端，创建并使用一个简单的 AI Agent。
// 百炼提供与 OpenAI Chat Completions API 兼容的接口。

using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ?? throw new InvalidOperationException("环境变量 DASHSCOPE_API_KEY 未设置。");
var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_CHAT_MODEL_NAME") ?? "qwen-plus";

// 阿里百炼提供 OpenAI 兼容的接口地址。
// 只需替换 Endpoint 和 API Key，即可无缝接入 Agent Framework。
AIAgent agent = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
    .GetChatClient(modelName)
    .AsAIAgent(instructions: "你是一个擅长讲笑话的助手。", name: "笑话王");

// 普通调用：等待完整结果后输出。
//Console.WriteLine(await agent.RunAsync("给我讲一个中国古代的寓言故事。").ConfigureAwait(false));

// 流式调用：逐步输出生成内容。
await foreach (var update in agent.RunStreamingAsync("给我讲一个中国古代的寓言故事。").ConfigureAwait(false))
{
    Console.WriteLine(update);
}
