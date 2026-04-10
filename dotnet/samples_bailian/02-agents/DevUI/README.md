# DevUI 示例

本目录包含演示如何在 ASP.NET Core 应用程序中使用 DevUI 与 AI 代理的示例。

## 示例内容

- **DevUI_Step01_BasicUsage** - 演示如何在 ASP.NET Core 应用程序中基本使用 DevUI，包括设置阿里百炼作为聊天客户端、创建函数工具、注册代理和工作流，以及访问 DevUI 界面。

## 技术说明

这些示例使用阿里百炼（DashScope）作为后端 LLM 服务，通过其与 OpenAI Chat Completions API 兼容的接口进行连接。

## 运行示例

1. 设置阿里百炼 API 密钥作为环境变量：
   - `DASHSCOPE_API_KEY` - 您的阿里百炼 API 密钥
   - `DASHSCOPE_CHAT_MODEL_NAME` - 您的模型名称（可选，默认为 "qwen-plus"）

2. 进入具体示例目录并运行：
   ```bash
   cd DevUI_Step01_BasicUsage
   dotnet run
   ```

3. 在浏览器中访问 DevUI 界面：
   ```
   https://localhost:50516/devui
   ```