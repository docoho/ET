using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 批量修改目标对象或组件上多个 SerializedProperty 序列化属性并自动保存/记录 Undo 的命令处理器（InspectorSetProperties）。
    /// </summary>
    internal sealed class UnityBridgeInspectorSetPropertiesHandler :
            AUnityBridgeInspectorHandler<InspectorSetPropertiesRequest, InspectorSetPropertiesResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorSetPropertiesRequest command)
        {
            await ETTask.CompletedTask;

            InspectorSetPropertiesResponse response = InspectorSetPropertiesResponse.Create();
            if (command.Values.Count == 0)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "Values cannot be empty";
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
                if (context.IsSceneObject)
                {
                    Undo.RecordObject(serializedTarget, "Set Serialized Properties");
                }

                foreach (BridgePropertyInfo value in command.Values)
                {
                    string propertyPath = !string.IsNullOrWhiteSpace(value.PropertyPath) ? value.PropertyPath : value.Name;
                    if (string.IsNullOrWhiteSpace(propertyPath))
                    {
                        response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                        response.Message = "property name cannot be empty";
                        return response;
                    }

                    SerializedProperty property = serializedObject.FindProperty(propertyPath);
                    if (property == null)
                    {
                        response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                        response.Message = $"property not found: {propertyPath}";
                        return response;
                    }

                    if (!property.editable)
                    {
                        response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                        response.Message = $"property is not editable: {propertyPath}";
                        return response;
                    }

                    if (!TrySetPropertyValue(property, value, out error))
                    {
                        response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                        response.Message = error;
                        return response;
                    }

                    response.Properties.Add(CreatePropertyInfo(property));
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
                return response;
            }
        }
    }
}
