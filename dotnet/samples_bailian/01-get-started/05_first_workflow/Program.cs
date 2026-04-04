// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI.Workflows;

namespace WorkflowExecutorsAndEdgesSample;

/// <summary>
/// 本示例介绍工作流中 Executor（执行器）和 Edge（边）的基本概念。
///
/// 工作流由多个 Executor（处理单元）通过 Edge（数据流路径）连接而成。
/// 本例创建了一个简单的文本处理流水线：
/// 1. UppercaseExecutor：将输入文本转换为大写
/// 2. ReverseTextExecutor：将大写文本反转
///
/// 两个 Executor 按顺序连接，数据依次流过每个节点。
/// 输入"你好，世界！"会依次经过大写转换（对中文无效，仅演示流程）再反转输出。
/// </summary>
public static class Program
{
    private static async Task Main()
    {
        // 创建第一个 Executor：将字符串转为大写（使用 Lambda 快速绑定）
        Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
        var uppercase = uppercaseFunc.BindAsExecutor("大写转换器");

        // 创建第二个 Executor：将字符串反转
        ReverseTextExecutor reverse = new();

        // 构建工作流：将两个 Executor 顺序连接，并声明最终输出来自 reverse
        WorkflowBuilder builder = new(uppercase);
        builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
        var workflow = builder.Build();

        // 执行工作流，传入输入数据
        Run run = await InProcessExecution.RunAsync(workflow, "Hello, Agent Framework!").ConfigureAwait(false);
        await using (run.ConfigureAwait(false))
        {
            foreach (WorkflowEvent evt in run.NewEvents)
            {
                // 每个 Executor 完成时会触发 ExecutorCompletedEvent，输出其处理结果
                if (evt is ExecutorCompletedEvent executorComplete)
                {
                    Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
                }
            }
        }
    }
}

/// <summary>
/// 第二个 Executor：将输入文本反转后作为工作流输出。
/// </summary>
internal sealed class ReverseTextExecutor() : Executor<string, string>("文本反转器")
{
    /// <summary>
    /// 将输入字符串逐字符反转后返回。
    /// </summary>
    /// <param name="message">待反转的输入文本</param>
    /// <param name="context">工作流上下文，可用于访问服务或添加事件</param>
    /// <param name="cancellationToken">取消令牌，默认为 <see cref="CancellationToken.None"/></param>
    /// <returns>反转后的文本</returns>
    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // 直接返回结果，框架会将其作为该 Executor 的输出事件发出。
        return ValueTask.FromResult(string.Concat(message.Reverse()));
    }
}
