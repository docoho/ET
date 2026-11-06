using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 触发编辑器撤销（Undo.PerformUndo）操作的命令处理器（EditorUndo）。
    /// </summary>
    internal sealed class UnityBridgeEditorUndoHandler : AUnityBridgeHandler<EditorUndoRequest, EditorUndoResponse>
    {
        protected override async ETTask<IResponse> Run(EditorUndoRequest command)
        {
            await ETTask.CompletedTask;

            int count = command.Count > 0 ? command.Count : 1;
            for (int i = 0; i < count; ++i)
            {
                Undo.PerformUndo();
            }

            EditorUndoResponse response = EditorUndoResponse.Create();
            response.Count = count;
            return response;
        }
    }
}
