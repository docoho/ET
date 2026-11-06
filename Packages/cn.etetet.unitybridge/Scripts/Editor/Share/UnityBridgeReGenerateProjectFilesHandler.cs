using System;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 执行 ET/Loader/ReGenerateProjectFiles 重新生成 csproj 工程文件并刷新资源的延迟命令处理器（RegenProject）。
    /// </summary>
    internal sealed class UnityBridgeReGenerateProjectFilesHandler : AUnityBridgeDeferredHandler<RegenProject, RegenProjectResponse>
    {
        protected override async ETTask<RegenProjectResponse> Run(RegenProject command, UnityBridgeDeferredContext deferred)
        {
            await ETTask.CompletedTask;
            
            if (!deferred.IsResuming && EditorApplication.isCompiling)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.Compiling, "unity is compiling");
            }

            if (!deferred.IsResuming)
            {
                if (!EditorApplication.ExecuteMenuItem("ET/Loader/ReGenerateProjectFiles"))
                {
                    throw new Exception("execute menu item failed: ET/Loader/ReGenerateProjectFiles");
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return deferred.Started<RegenProjectResponse>();
            }

            if (EditorApplication.isCompiling)
            {
                return deferred.NotReady<RegenProjectResponse>();
            }

            RegenProjectResponse response = RegenProjectResponse.Create();
            if (EditorUtility.scriptCompilationFailed)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = "unity regenerate project files finished with compile errors";
                return response;
            }

            response.Message = "unity regenerate project files completed";
            return response;
        }
    }
}
