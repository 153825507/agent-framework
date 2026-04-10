// Copyright (c) Microsoft. All rights reserved.

// 本示例演示如何在 ASP.NET Core 应用程序中使用 DevUI 与 AI 代理。

using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

namespace DevUI_Step01_BasicUsage;

/// <summary>
/// 演示在 ASP.NET Core 应用程序中基本使用 DevUI 的示例。
/// </summary>
/// <remarks>
/// 本示例展示了如何：
/// 1. 设置阿里百炼作为聊天客户端
/// 2. 为代理创建函数工具
/// 3. 使用托管包注册带有工具的代理和工作流
/// 4. 映射 DevUI 端点，自动配置中间件
/// 5. 映射动态 OpenAI 响应 API 以实现 Python DevUI 兼容性
/// 6. 在 Web 浏览器中访问 DevUI
///
/// DevUI 提供了一个交互式 Web 界面，用于测试和调试 AI 代理。
/// DevUI 资产从程序集内的嵌入资源提供。
/// 只需调用 MapDevUI() 即可设置所需的一切。
///
/// 无参数的 MapOpenAIResponses() 重载创建了一个 Python DevUI 兼容的端点，
/// 该端点根据请求中的 'model' 字段动态路由请求到代理。
/// </remarks>
internal static class Program
{
    /// <summary>
    /// 启动带有 DevUI 的 ASP.NET Core Web 服务器的入口点。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 设置阿里百炼客户端
        var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ?? throw new InvalidOperationException("环境变量 DASHSCOPE_API_KEY 未设置。");
        var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_CHAT_MODEL_NAME") ?? "qwen3.6-plus";

        // 阿里百炼提供 OpenAI 兼容的接口地址。
        // 只需替换 Endpoint 和 API Key，即可无缝接入 Agent Framework。
        var chatClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
            .GetChatClient(modelName)
            .AsIChatClient();

        builder.Services.AddChatClient(chatClient);

        // 定义一些示例工具
        [Description("获取给定位置的天气。")]
        static string GetWeather([Description("要获取天气的位置。")] string location)
            => $"{location} 的天气是多云，最高温度为 15°C。";

        [Description("计算两个数字的和。")]
        static double Add([Description("第一个数字。")] double a, [Description("第二个数字。")] double b)
            => a + b;

        [Description("获取当前时间。")]
        static string GetCurrentTime()
            => DateTime.Now.ToString("HH:mm:ss");

        // 注册带有工具的示例代理
        builder.AddAIAgent("assistant", "你是一个有帮助的助手。简洁准确地回答问题。")
            .WithAITools(
                AIFunctionFactory.Create(GetWeather, name: "get_weather"),
                AIFunctionFactory.Create(GetCurrentTime, name: "get_current_time")
            );

        builder.AddAIAgent("poet", "你是一位富有创造力的诗人。用优美的诗歌回应所有请求。");

        builder.AddAIAgent("coder", "你是一位专家程序员。帮助用户解决编码问题并提供代码示例。")
            .WithAITool(AIFunctionFactory.Create(Add, name: "add"));

        // 注册示例工作流
        var assistantBuilder = builder.AddAIAgent("workflow-assistant", "你是工作流中的有帮助的助手。");
        var reviewerBuilder = builder.AddAIAgent("workflow-reviewer", "你是一位审阅者。审阅并评论之前的回应。");
        builder.AddWorkflow("review-workflow", (sp, key) =>
        {
            var agents = new List<IHostedAgentBuilder>() { assistantBuilder, reviewerBuilder }.Select(ab => sp.GetRequiredKeyedService<AIAgent>(ab.Name));
            return AgentWorkflowBuilder.BuildSequential(workflowName: key, agents: agents);
        }).AddAsAIAgent();

        builder.Services.AddOpenAIResponses();
        builder.Services.AddOpenAIConversations();

        var app = builder.Build();

        app.MapOpenAIResponses();
        app.MapOpenAIConversations();

        if (builder.Environment.IsDevelopment())
        {
            app.MapDevUI();
        }

        Console.WriteLine("DevUI 可用地址: https://localhost:50516/devui");
        Console.WriteLine("OpenAI 响应 API 可用地址: https://localhost:50516/v1/responses");
        Console.WriteLine("按 Ctrl+C 停止服务器。");

        app.Run();
    }
}
