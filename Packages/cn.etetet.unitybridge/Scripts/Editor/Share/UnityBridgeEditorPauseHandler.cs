using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 控制或切换 Unity Editor 暂停状态（EditorApplication.isPaused）的命令处理器（EditorPause）。
    /// </summary>
    internal sealed class UnityBridgeEditorPauseHandler : AUnityBridgeHandler<EditorPauseRequest, EditorPauseResponse>
    {
        protected override async ETTask<IResponse> Run(EditorPauseRequest command)
        {
            await ETTask.CompletedTask;

            if (command.Toggle)
            {
                EditorApplication.isPaused = !EditorApplication.isPaused;
            }
            else
            {
                EditorApplication.isPaused = command.Pause;
            }

            EditorPauseResponse response = EditorPauseResponse.Create();
            response.IsPaused = EditorApplication.isPaused;
            return response;
        }
    }
}
