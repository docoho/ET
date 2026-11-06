using System;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 控制 Unity Editor 进入运行模式（PlayMode）并跨状态切换等待就绪的延迟命令处理器（EnterPlay）。
    /// </summary>
    internal sealed class UnityBridgeEnterPlayModeHandler : AUnityBridgeDeferredHandler<EnterPlay, EnterPlayResponse>
    {
        protected override async ETTask<EnterPlayResponse> Run(EnterPlay command, UnityBridgeDeferredContext deferred)
        {
            await ETTask.CompletedTask;
            
            if (!deferred.IsResuming && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.AlreadyInPlayMode, "unity already in playmode or changing playmode");
            }

            if (!deferred.IsResuming && EditorApplication.isCompiling)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.Compiling, "unity is compiling");
            }

            if (!deferred.IsResuming)
            {
                EditorApplication.isPlaying = true;
                return deferred.Started<EnterPlayResponse>();
            }

            if (EditorApplication.isPlaying)
            {
                EnterPlayResponse response = EnterPlayResponse.Create();
                response.Message = "unity entered playmode";
                response.IsPlaying = true;
                return response;
            }

            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return deferred.NotReady<EnterPlayResponse>();
            }

            EnterPlayResponse failure = EnterPlayResponse.Create();
            failure.IsPlaying = false;
            failure.Error = UnityBridgeErrorCode.HandlerFail;
            failure.Message = EditorUtility.scriptCompilationFailed
                    ? "enter playmode failed because scripts failed to compile"
                    : "unity failed to enter playmode";
            return failure;
        }
    }
}
