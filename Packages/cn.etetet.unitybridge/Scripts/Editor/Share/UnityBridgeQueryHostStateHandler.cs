using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 查询编辑器宿主完整状态及所有已注册可用命令名称列表的命令处理器（HostState）。
    /// </summary>
    internal sealed class UnityBridgeQueryHostStateHandler : AUnityBridgeHandler<HostState, HostStateResponse>
    {
        protected override async ETTask<IResponse> Run(HostState command)
        {
            await ETTask.CompletedTask;
            HostStateResponse response = HostStateResponse.Create();
            response.IsCompiling = EditorApplication.isCompiling;
            response.IsPlaying = EditorApplication.isPlaying;
            response.IsPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode;
            response.CodeMode = UnityBridgeEditorStatus.GetCodeMode();
            response.UnityVersion = Application.unityVersion;
            response.AvailableCommands = string.Join(",", UnityBridgeEditorDispatcher.GetAvailableCommandTypes());
            return response;
        }
    }
}
