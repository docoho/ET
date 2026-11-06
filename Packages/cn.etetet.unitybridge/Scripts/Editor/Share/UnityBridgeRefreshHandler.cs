using System;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 强制刷新 AssetDatabase（ImportAssetOptions.ForceUpdate）并跨编译等待完成的延迟命令处理器（Refresh）。
    /// </summary>
    internal sealed class UnityBridgeRefreshHandler : AUnityBridgeDeferredHandler<Refresh, RefreshResponse>
    {
        protected override async ETTask<RefreshResponse> Run(Refresh command, UnityBridgeDeferredContext deferred)
        {
            await ETTask.CompletedTask;
            
            if (!deferred.IsResuming && EditorApplication.isCompiling)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.Compiling, "unity is compiling");
            }

            if (!deferred.IsResuming)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return deferred.Started<RefreshResponse>();
            }

            if (EditorApplication.isCompiling)
            {
                return deferred.NotReady<RefreshResponse>();
            }

            RefreshResponse response = RefreshResponse.Create();
            if (EditorUtility.scriptCompilationFailed)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = "unity refresh finished with compile errors";
                return response;
            }

            response.Message = "unity refresh completed";
            return response;
        }
    }
}
