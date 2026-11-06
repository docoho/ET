using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 修改目标对象或组件上单个 SerializedProperty 序列化属性值并自动保存/记录 Undo 的命令处理器（InspectorSetProperty）。
    /// </summary>
    internal sealed class UnityBridgeInspectorSetPropertyHandler :
            AUnityBridgeInspectorHandler<InspectorSetPropertyRequest, InspectorSetPropertyResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorSetPropertyRequest command)
        {
            await ETTask.CompletedTask;

            InspectorSetPropertyResponse response = InspectorSetPropertyResponse.Create();
            if (string.IsNullOrWhiteSpace(command.PropertyName))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "PropertyName is required";
                return response;
            }

            if (command.Value == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "Value is required";
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
                serializedObject.Update();
                SerializedProperty property = serializedObject.FindProperty(command.PropertyName);
                if (property == null)
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = $"property not found: {command.PropertyName}";
                    return response;
                }

                if (!property.editable)
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = $"property is not editable: {command.PropertyName}";
                    return response;
                }

                if (context.IsSceneObject)
                {
                    Undo.RecordObject(serializedTarget, "Set Serialized Property");
                }

                if (!TrySetPropertyValue(property, command.Value, out error))
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = error;
                    return response;
                }

                response.Changed = serializedObject.ApplyModifiedProperties();
                if (response.Changed && !TrySaveModifiedTarget(context, serializedTarget, out error))
                {
                    response.Error = UnityBridgeErrorCode.HandlerFail;
                    response.Message = error;
                    return response;
                }

                response.TargetName = serializedTarget.name;
                response.TargetType = serializedTarget.GetType().Name;
                response.GameObjectName = context.GameObject == null ? string.Empty : context.GameObject.name;
                response.ComponentName = component == null ? string.Empty : component.GetType().Name;
                response.AssetPath = context.AssetPath;
                response.ObjectPath = context.ObjectPath;
                response.Properties.Add(CreatePropertyInfo(property));
                return response;
            }
        }
    }
}
