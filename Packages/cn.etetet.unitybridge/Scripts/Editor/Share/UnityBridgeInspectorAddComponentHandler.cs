using System;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 向场景对象或 Prefab 资产的目标节点挂载指定类型组件的命令处理器（InspectorAddComponent）。
    /// </summary>
    internal sealed class UnityBridgeInspectorAddComponentHandler :
            AUnityBridgeInspectorHandler<InspectorAddComponentRequest, InspectorAddComponentResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorAddComponentRequest command)
        {
            await ETTask.CompletedTask;

            InspectorAddComponentResponse response = InspectorAddComponentResponse.Create();
            if (string.IsNullOrWhiteSpace(command.TypeName))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "TypeName is required";
                return response;
            }

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

                Type componentType = ResolveComponentType(command.TypeName);
                if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = $"component type not found: {command.TypeName}";
                    return response;
                }

                Component component = context.IsSceneObject
                        ? Undo.AddComponent(context.GameObject, componentType)
                        : context.GameObject.AddComponent(componentType);

                if (!context.IsSceneObject && !TrySaveModifiedTarget(context, component, out error))
                {
                    response.Error = UnityBridgeErrorCode.HandlerFail;
                    response.Message = error;
                    return response;
                }

                response.GameObjectName = context.GameObject.name;
                response.AssetPath = context.AssetPath;
                response.ObjectPath = context.ObjectPath;
                response.AddedComponent = CreateComponentInfo(component, Array.IndexOf(context.GameObject.GetComponents<Component>(), component));
                return response;
            }
        }
    }
}
