using System;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 连通性探活命令处理器（Ping），返回当前时间戳、编译状态、PlayMode 状态、CodeMode 与 Unity 版本号。
    /// </summary>
    internal sealed class UnityBridgePingHandler : AUnityBridgeHandler<Ping, PingResponse>
    {
        protected override async ETTask<IResponse> Run(Ping command)
        {
            await ETTask.CompletedTask;
            PingResponse response = PingResponse.Create();
            response.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            response.IsCompiling = EditorApplication.isCompiling;
            response.IsPlaying = EditorApplication.isPlaying;
            response.IsPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode;
            response.CodeMode = UnityBridgeEditorStatus.GetCodeMode();
            response.UnityVersion = Application.unityVersion;
            return response;
        }
    }
}
