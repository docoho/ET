using System;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 执行菜单项 ET/Scripts/Compile 触发脚本编译并跨 Domain Reload 等待编译完成的延迟命令处理器（Compile）。
    /// </summary>
    internal sealed class UnityBridgeCompileScriptsHandler : AUnityBridgeDeferredHandler<Compile, CompileResponse>
    {
        protected override async ETTask<CompileResponse> Run(Compile command, UnityBridgeDeferredContext deferred)
        {
            await ETTask.CompletedTask;
            
            if (!deferred.IsResuming && EditorApplication.isCompiling)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.Compiling, "unity is compiling");
            }

            if (!deferred.IsResuming)
            {
                if (!EditorApplication.ExecuteMenuItem("ET/Scripts/Compile"))
                {
                    throw new Exception("execute menu item failed: ET/Scripts/Compile");
                }

                return deferred.Started<CompileResponse>();
            }

            if (EditorApplication.isCompiling)
            {
                return deferred.NotReady<CompileResponse>();
            }

            CompileResponse response = CompileResponse.Create();
            if (EditorUtility.scriptCompilationFailed)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = "unity compile finished with compile errors";
                return response;
            }

            response.Message = "unity compile completed";
            response.DurationMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - deferred.StartedAt;
            return response;
        }
    }
}
