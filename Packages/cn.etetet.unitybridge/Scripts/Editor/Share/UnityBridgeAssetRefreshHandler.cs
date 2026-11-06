using System;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 触发 AssetDatabase.Refresh 并等待资源刷新及潜在脚本编译完成的延迟命令处理器（AssetRefresh）。
    /// </summary>
    internal sealed class UnityBridgeAssetRefreshHandler : AUnityBridgeDeferredHandler<AssetRefreshRequest, AssetRefreshResponse>
    {
        protected override async ETTask<AssetRefreshResponse> Run(AssetRefreshRequest command, UnityBridgeDeferredContext deferred)
        {
            await ETTask.CompletedTask;
            
            if (!deferred.IsResuming && EditorApplication.isCompiling)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.Compiling, "unity is compiling");
            }

            if (!deferred.IsResuming)
            {
                AssetDatabase.Refresh(ToImportOptions(command.ForceUpdate));
                return deferred.Started<AssetRefreshResponse>();
            }

            if (EditorApplication.isCompiling)
            {
                return deferred.NotReady<AssetRefreshResponse>();
            }

            AssetRefreshResponse response = AssetRefreshResponse.Create();
            response.Refreshed = true;
            if (EditorUtility.scriptCompilationFailed)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = "asset refresh finished with compile errors";
                return response;
            }

            response.Message = "asset refresh completed";
            return response;
        }

        private static ImportAssetOptions ToImportOptions(bool forceUpdate)
        {
            return forceUpdate ? ImportAssetOptions.ForceUpdate : ImportAssetOptions.Default;
        }
    }
}
