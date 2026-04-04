// Copyright (c) Microsoft. All rights reserved.

// 本示例演示如何为 Agent 添加函数工具（Function Tools）。
// 展示了普通调用和流式调用两种方式，工具示例为查询天气。

using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ?? throw new InvalidOperationException("环境变量 DASHSCOPE_API_KEY 未设置。");
var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_CHAT_MODEL_NAME") ?? "qwen-plus";

// 定义一个天气查询工具函数，Description 特性用于告知模型该函数的用途。
[Description("查询指定城市的天气情况。")]
static string GetWeather([Description("要查询天气的城市名称。")] string location)
    => $"{location}的天气：多云，最高气温 15°C。";

// 创建 Agent，并将天气工具注册进去。
// 阿里百炼提供 OpenAI 兼容的接口，支持 Function Calling。
AIAgent agent = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
    .GetChatClient(modelName)
    .AsAIAgent(instructions: "你是一个有帮助的助手。", tools: [AIFunctionFactory.Create(GetWeather)]);

// 普通调用：等待完整结果后输出。
Console.WriteLine(await agent.RunAsync("北京今天天气怎么样？").ConfigureAwait(false));

// 流式调用：逐步输出生成内容。
await foreach (var update in agent.RunStreamingAsync("上海今天天气怎么样？").ConfigureAwait(false))
{
    Console.WriteLine(update);
}
