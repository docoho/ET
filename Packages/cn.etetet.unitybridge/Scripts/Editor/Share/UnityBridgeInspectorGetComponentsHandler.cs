using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 获取目标场景对象或 Prefab 节点上挂载的所有组件列表信息的命令处理器（InspectorGetComponents）。
    /// </summary>
    internal sealed class UnityBridgeInspectorGetComponentsHandler :
            AUnityBridgeInspectorHandler<InspectorGetComponentsRequest, InspectorGetComponentsResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorGetComponentsRequest command)
        {
            await ETTask.CompletedTask;

            InspectorGetComponentsResponse response = InspectorGetComponentsResponse.Create();
            if (!TryResolveTargetContext(command.Path, command.InstanceId, command.AssetPath, command.ObjectPath, false, out TargetContext context, out string error))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = error;
                return response;
            }

            using (context)
            {
                if (context.GameObject == null)
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = "target does not contain components";
                    return response;
                }

                Component[] components = context.GameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; ++i)
                {
                    if (components[i] != null)
                    {
                        response.Components.Add(CreateComponentInfo(components[i], i));
                    }
                }

                response.GameObjectName = context.GameObject.name;
                response.AssetPath = context.AssetPath;
                response.ObjectPath = context.ObjectPath;
                response.Count = response.Components.Count;
                return response;
            }
        }
    }
}
