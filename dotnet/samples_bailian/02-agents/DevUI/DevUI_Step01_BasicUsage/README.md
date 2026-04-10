# DevUI 步骤 01 - 基本使用

本示例演示如何在 ASP.NET Core 应用程序中添加 DevUI 与 AI 代理。

## 什么是 DevUI？

DevUI 提供了一个交互式 Web 界面，用于在开发过程中测试和调试 AI 代理。

## 配置

设置以下环境变量：

- `DASHSCOPE_API_KEY` - 您的阿里百炼 API 密钥（必需）
- `DASHSCOPE_CHAT_MODEL_NAME` - 您的模型名称（默认为 "qwen-plus"）

## 运行示例

1. 将阿里百炼凭据设置为环境变量
2. 运行应用程序：
   ```bash
   dotnet run
   ```
3. 在浏览器中打开 https://localhost:50516/devui
4. 从下拉菜单中选择一个代理或工作流并开始聊天！

## 示例代理和工作流

本示例包括：

**代理：**
- **assistant** - 有帮助的助手
- **poet** - 富有创造力的诗人
- **coder** - 专家程序员

**工作流：**
- **review-workflow** - 一个顺序工作流，先生成回应，然后进行审阅

## 在您自己的项目中添加 DevUI

要在您的 ASP.NET Core 应用程序中添加 DevUI：

1. 添加 DevUI 包和托管包：
   ```bash
   dotnet add package Microsoft.Agents.AI.DevUI
   dotnet add package Microsoft.Agents.AI.Hosting
   dotnet add package Microsoft.Agents.AI.Hosting.OpenAI
   dotnet add package OpenAI
   ```

2. 注册您的代理和工作流：
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   
   // 设置阿里百炼客户端
   var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ?? throw new InvalidOperationException("环境变量 DASHSCOPE_API_KEY 未设置。");
   var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_CHAT_MODEL_NAME") ?? "qwen-plus";

   var chatClient = new OpenAIClient(
       new ApiKeyCredential(apiKey),
       new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
       .GetChatClient(modelName)
       .AsIChatClient();
   
   builder.Services.AddChatClient(chatClient);
   
   // 注册代理
   builder.AddAIAgent("assistant", "你是一个有帮助的助手。");
   
   // 注册工作流
   var agent1Builder = builder.AddAIAgent("workflow-agent1", "你是代理 1。");
   var agent2Builder = builder.AddAIAgent("workflow-agent2", "你是代理 2。");
   builder.AddSequentialWorkflow("my-workflow", [agent1Builder, agent2Builder])
       .AddAsAIAgent();
   ```

3. 添加 OpenAI 服务并映射 OpenAI 和 DevUI 的端点：
   ```csharp
   // 注册 OpenAI 响应和对话服务（DevUI 也需要）
   builder.Services.AddOpenAIResponses();
   builder.Services.AddOpenAIConversations();

   var app = builder.Build();

   // 映射 OpenAI 响应和对话的端点（DevUI 也需要）
   app.MapOpenAIResponses();
   app.MapOpenAIConversations();

   if (builder.Environment.IsDevelopment())
   {
       // 将 DevUI 端点映射到 /devui
       app.MapDevUI();
   }
   
   app.Run();
   ```

4. 在浏览器中导航到 `/devui`