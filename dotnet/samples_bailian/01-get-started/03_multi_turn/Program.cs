// Copyright (c) Microsoft. All rights reserved.

// 本示例演示如何使用 AgentSession 实现多轮对话。
// 通过 Session 对象在多次调用之间保持上下文，使 Agent 能记住前面的内容。

using System.ClientModel;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;

var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ?? throw new InvalidOperationException("环境变量 DASHSCOPE_API_KEY 未设置。");
var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_CHAT_MODEL_NAME") ?? "qwen-plus";

// 创建 Agent，使用阿里百炼兼容接口。
AIAgent agent = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
    .GetChatClient(modelName)
    .AsAIAgent(instructions: "你是一个擅长讲笑话的助手。", name: "笑话王");

// 普通多轮对话：Session 保存上下文，第二轮可以引用第一轮的内容。
AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
Console.WriteLine(await agent.RunAsync("给我讲一个关于程序员的笑话。", session).ConfigureAwait(false));
Console.WriteLine(await agent.RunAsync("把这个笑话改成古文风格再讲一遍。", session).ConfigureAwait(false));

// 流式多轮对话：同样通过 Session 保持上下文。
session = await agent.CreateSessionAsync().ConfigureAwait(false);
await foreach (var update in agent.RunStreamingAsync("给我讲一个关于程序员的笑话。", session).ConfigureAwait(false))
{
    Console.WriteLine(update);
}
await foreach (var update in agent.RunStreamingAsync("把这个笑话改成古文风格再讲一遍。", session).ConfigureAwait(false))
{
    Console.WriteLine(update);
}
