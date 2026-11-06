using System;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 从目标场景对象或 Prefab 节点移除指定组件的命令处理器（InspectorRemoveComponent）。
    /// </summary>
    internal sealed class UnityBridgeInspectorRemoveComponentHandler :
            AUnityBridgeInspectorHandler<InspectorRemoveComponentRequest, InspectorRemoveComponentResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorRemoveComponentRequest command)
        {
            await ETTask.CompletedTask;

            InspectorRemoveComponentResponse response = InspectorRemoveComponentResponse.Create();
            if (!TryResolveTargetContext(command.Path, command.InstanceId, command.AssetPath, command.ObjectPath, true, out TargetContext context, out string error))
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

                Component component = ResolveComponent(context, command.ComponentName, command.ComponentIndex, command.ComponentInstanceId);
                if (component == null)
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = "Component not found";
                    return response;
                }

                if (component is Transform)
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = "cannot remove Transform component";
                    return response;
                }

                response.GameObjectName = context.GameObject.name;
                response.AssetPath = context.AssetPath;
                response.ObjectPath = context.ObjectPath;
                response.RemovedComponent = CreateComponentInfo(component, Array.IndexOf(context.GameObject.GetComponents<Component>(), component));

                if (context.IsSceneObject)
                {
                    Undo.DestroyObjectImmediate(component);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(component, true);
                    if (!TrySaveModifiedTarget(context, context.GameObject, out error))
                    {
                        response.Error = UnityBridgeErrorCode.HandlerFail;
                        response.Message = error;
                        return response;
                    }
                }

                return response;
            }
        }
    }
}
