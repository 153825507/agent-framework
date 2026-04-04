// Copyright (c) Microsoft. All rights reserved.

// 本示例演示如何为 Agent 添加自定义记忆组件（AIContextProvider）。
// 记忆组件会监听对话中的所有消息，并从中提取用户的姓名和年龄。
// 如果尚未获取这些信息，组件会主动向用户询问；
// 一旦获取后，会在每次调用前将其注入到模型上下文中。

using System.ClientModel;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using SampleApp;

var apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY") ?? throw new InvalidOperationException("环境变量 DASHSCOPE_API_KEY 未设置。");
var modelName = Environment.GetEnvironmentVariable("DASHSCOPE_CHAT_MODEL_NAME") ?? "qwen-plus";

// 创建底层 ChatClient，使用阿里百炼兼容接口。
ChatClient chatClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1") })
    .GetChatClient(modelName);

// 创建 Agent，并注册自定义记忆组件。
// 每个新 Session 都有独立的记忆对象，互不干扰。
// 实际生产场景中，建议将用户信息持久化到数据库，并按用户 ID 隔离存储。
AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatOptions = new() { Instructions = "你是一个友好的助手，始终用用户的姓名称呼对方。" },
    AIContextProviders = [new UserInfoMemory(chatClient.AsIChatClient())]
});

// 创建新会话。
AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

Console.WriteLine(">> 使用空白记忆的新会话\n");

// 普通多轮对话：记忆组件会从对话中提取姓名和年龄。
Console.WriteLine(await agent.RunAsync("你好，9 的平方根是多少？", session).ConfigureAwait(false));
Console.WriteLine(await agent.RunAsync("我叫小明。", session).ConfigureAwait(false));
Console.WriteLine(await agent.RunAsync("我今年 25 岁。", session).ConfigureAwait(false));

// 序列化 Session，序列化结果中包含记忆组件的状态。
JsonElement sessionElement = await agent.SerializeSessionAsync(session).ConfigureAwait(false);

Console.WriteLine("\n>> 使用反序列化后的 Session（恢复之前的记忆）\n");

// 反序列化 Session，继续携带之前已提取的记忆继续对话。
var deserializedSession = await agent.DeserializeSessionAsync(sessionElement).ConfigureAwait(false);
Console.WriteLine(await agent.RunAsync("我的姓名和年龄是什么？", deserializedSession).ConfigureAwait(false));

Console.WriteLine("\n>> 直接读取记忆组件中存储的内容\n");

// 通过 GetService 访问记忆组件，直接读取提取到的用户信息。
var userInfo = agent.GetService<UserInfoMemory>()?.GetUserInfo(deserializedSession);

Console.WriteLine($"记忆 - 用户姓名：{userInfo?.UserName}");
Console.WriteLine($"记忆 - 用户年龄：{userInfo?.UserAge}");

Console.WriteLine("\n>> 在新会话中复用已有记忆\n");

// 也可以在新 Session 中手动注入已有记忆，实现跨 Session 共享。
var newSession = await agent.CreateSessionAsync().ConfigureAwait(false);
if (userInfo is not null && agent.GetService<UserInfoMemory>() is UserInfoMemory newSessionMemory)
{
    newSessionMemory.SetUserInfo(newSession, userInfo);
}

// 此时新 Session 已拥有之前提取的用户信息，Agent 应能正确响应。
Console.WriteLine(await agent.RunAsync("我的姓名和年龄是什么？", newSession).ConfigureAwait(false));

namespace SampleApp
{
    /// <summary>
    /// 自定义记忆组件，用于记住用户的姓名和年龄。
    /// </summary>
    internal sealed class UserInfoMemory : AIContextProvider
    {
        private readonly ProviderSessionState<UserInfo> _sessionState;
        private IReadOnlyList<string>? _stateKeys;
        private readonly IChatClient _chatClient;

        public UserInfoMemory(IChatClient chatClient, Func<AgentSession?, UserInfo>? stateInitializer = null)
        {
            this._sessionState = new ProviderSessionState<UserInfo>(
                stateInitializer ?? (_ => new UserInfo()),
                this.GetType().Name);
            this._chatClient = chatClient;
        }

        public override IReadOnlyList<string> StateKeys => this._stateKeys ??= [this._sessionState.StateKey];

        public UserInfo GetUserInfo(AgentSession session)
            => this._sessionState.GetOrInitializeState(session);

        public void SetUserInfo(AgentSession session, UserInfo userInfo)
            => this._sessionState.SaveState(session, userInfo);

        /// <summary>
        /// 每次对话结束后，尝试从消息中提取用户姓名和年龄并存入记忆。
        /// </summary>
        protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
        {
            var userInfo = this._sessionState.GetOrInitializeState(context.Session);

            // 仅在姓名或年龄未知时，且存在用户消息时，才调用模型提取。
            if ((userInfo.UserName is null || userInfo.UserAge is null) && context.RequestMessages.Any(x => x.Role == ChatRole.User))
            {
                var result = await this._chatClient.GetResponseAsync<UserInfo>(
                    context.RequestMessages,
                    new ChatOptions()
                    {
                        Instructions = "从消息中提取用户的姓名和年龄，如果没有则返回 null。"
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                userInfo.UserName ??= result.Result.UserName;
                userInfo.UserAge ??= result.Result.UserAge;
            }

            this._sessionState.SaveState(context.Session, userInfo);
        }

        /// <summary>
        /// 每次调用前，将已知的用户信息注入到模型上下文中。
        /// </summary>
        protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
        {
            var userInfo = this._sessionState.GetOrInitializeState(context.Session);

            StringBuilder instructions = new();

            // 若姓名或年龄未知，则提示模型向用户询问；否则直接提供已知信息。
            instructions
                .AppendLine(
                    userInfo.UserName is null ?
                        "请先询问用户的姓名，未获取前礼貌地拒绝回答其他问题。" :
                        $"用户的姓名是 {userInfo.UserName}。")
                .AppendLine(
                    userInfo.UserAge is null ?
                        "请先询问用户的年龄，未获取前礼貌地拒绝回答其他问题。" :
                        $"用户的年龄是 {userInfo.UserAge} 岁。");

            return new ValueTask<AIContext>(new AIContext
            {
                Instructions = instructions.ToString()
            });
        }
    }

    internal sealed class UserInfo
    {
        public string? UserName { get; set; }
        public int? UserAge { get; set; }
    }
}
