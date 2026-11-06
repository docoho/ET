using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 遍历读取目标对象或指定组件上全部可见 SerializedProperty 序列化属性的命令处理器（InspectorGetProperties）。
    /// </summary>
    internal sealed class UnityBridgeInspectorGetPropertiesHandler :
            AUnityBridgeInspectorHandler<InspectorGetPropertiesRequest, InspectorGetPropertiesResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorGetPropertiesRequest command)
        {
            await ETTask.CompletedTask;

            InspectorGetPropertiesResponse response = InspectorGetPropertiesResponse.Create();
            if (!TryResolveTargetContext(command.Path, command.InstanceId, command.AssetPath, command.ObjectPath, false, out TargetContext context, out string error))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = error;
                return response;
            }

            using (context)
            {
                if (!TryResolveSerializedTarget(
                        context,
                        command.ComponentName,
                        command.ComponentIndex,
                        command.ComponentInstanceId,
                        out UnityEngine.Object serializedTarget,
                        out Component component,
                        out error))
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = error;
                    return response;
                }

                SerializedObject serializedObject = new(serializedTarget);
                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = command.IncludeChildren;
                    response.Properties.Add(CreatePropertyInfo(iterator));
                }

                response.TargetName = serializedTarget.name;
                response.TargetType = serializedTarget.GetType().Name;
                response.GameObjectName = context.GameObject == null ? string.Empty : context.GameObject.name;
                response.ComponentName = component == null ? string.Empty : component.GetType().Name;
                response.AssetPath = context.AssetPath;
                response.ObjectPath = context.ObjectPath;
                return response;
            }
        }
    }
}
