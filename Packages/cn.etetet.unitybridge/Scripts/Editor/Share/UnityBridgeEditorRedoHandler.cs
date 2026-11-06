using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 触发编辑器重做（Undo.PerformRedo）操作的命令处理器（EditorRedo）。
    /// </summary>
    internal sealed class UnityBridgeEditorRedoHandler : AUnityBridgeHandler<EditorRedoRequest, EditorRedoResponse>
    {
        protected override async ETTask<IResponse> Run(EditorRedoRequest command)
        {
            await ETTask.CompletedTask;

            int count = command.Count > 0 ? command.Count : 1;
            for (int i = 0; i < count; ++i)
            {
                Undo.PerformRedo();
            }

            EditorRedoResponse response = EditorRedoResponse.Create();
            response.Count = count;
            return response;
        }
    }
}
