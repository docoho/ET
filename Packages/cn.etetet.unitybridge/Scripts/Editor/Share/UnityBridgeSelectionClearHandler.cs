using System;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// UnityBridge 清空当前编辑器选中集合（Selection.objects）的命令处理器（SelectionClear）。
    /// </summary>
    internal sealed class UnityBridgeSelectionClearHandler : AUnityBridgeHandler<SelectionClearRequest, SelectionClearResponse>
    {
        protected override async ETTask<IResponse> Run(SelectionClearRequest command)
        {
            await ETTask.CompletedTask;

            Selection.objects = Array.Empty<UnityEngine.Object>();
            Selection.activeObject = null;

            SelectionClearResponse response = SelectionClearResponse.Create();
            response.Cleared = true;
            response.SelectedCount = 0;
            return response;
        }
    }
}
