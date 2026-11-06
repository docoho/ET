using System;

namespace ET
{
    /// <summary>
    /// UnityBridge 测试回显命令处理器（TestEcho），原样返回请求文本与处理时间戳，用于桥接通道自检。
    /// </summary>
    public sealed class UnityBridgeTestEchoHandler : AUnityBridgeHandler<TestEcho, TestEchoResponse>
    {
        protected override async ETTask<IResponse> Run(TestEcho command)
        {
            await ETTask.CompletedTask;
            TestEchoResponse response = TestEchoResponse.Create();
            response.Text = command.Text;
            response.HandledAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            response.Handler = nameof(UnityBridgeTestEchoHandler);
            return response;
        }
    }
}
