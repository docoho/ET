using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 读取目标对象或组件上单个指定名称/路径 SerializedProperty 序列化属性值的命令处理器（InspectorGetProperty）。
    /// </summary>
    internal sealed class UnityBridgeInspectorGetPropertyHandler :
            AUnityBridgeInspectorHandler<InspectorGetPropertyRequest, InspectorGetPropertyResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorGetPropertyRequest command)
        {
            await ETTask.CompletedTask;

            InspectorGetPropertyResponse response = InspectorGetPropertyResponse.Create();
            if (string.IsNullOrWhiteSpace(command.PropertyName))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "PropertyName is required";
                return response;
            }

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
                SerializedProperty property = serializedObject.FindProperty(command.PropertyName);
                if (property == null)
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = $"property not found: {command.PropertyName}";
                    return response;
                }

                response.TargetName = serializedTarget.name;
                response.TargetType = serializedTarget.GetType().Name;
                response.ComponentName = component == null ? string.Empty : component.GetType().Name;
                response.AssetPath = context.AssetPath;
                response.ObjectPath = context.ObjectPath;
                response.Property = CreatePropertyInfo(property);
                return response;
            }
        }
    }
}
