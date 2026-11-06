using System;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 控制 Unity Editor 退出运行模式（PlayMode）并等待返回编辑模式的延迟命令处理器（ExitPlay）。
    /// </summary>
    internal sealed class UnityBridgeExitPlayModeHandler : AUnityBridgeDeferredHandler<ExitPlay, ExitPlayResponse>
    {
        protected override async ETTask<ExitPlayResponse> Run(ExitPlay command, UnityBridgeDeferredContext deferred)
        {
            await ETTask.CompletedTask;
            
            if (!deferred.IsResuming && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.NotInPlayMode, "unity not in playmode");
            }

            if (!deferred.IsResuming)
            {
                EditorApplication.isPlaying = false;
                return deferred.Started<ExitPlayResponse>();
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return deferred.NotReady<ExitPlayResponse>();
            }

            ExitPlayResponse response = ExitPlayResponse.Create();
            if (!EditorApplication.isPlaying)
            {
                response.Message = "unity exited playmode";
                response.IsPlaying = false;
                return response;
            }

            response.Error = UnityBridgeErrorCode.HandlerFail;
            response.Message = "unity failed to exit playmode";
            response.IsPlaying = true;
            return response;
        }
    }
}
