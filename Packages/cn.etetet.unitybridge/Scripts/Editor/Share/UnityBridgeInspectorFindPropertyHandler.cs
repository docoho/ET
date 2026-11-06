using System;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 按关键字搜索目标对象或组件上匹配的 SerializedProperty 序列化属性列表的命令处理器（InspectorFindProperty）。
    /// </summary>
    internal sealed class UnityBridgeInspectorFindPropertyHandler :
            AUnityBridgeInspectorHandler<InspectorFindPropertyRequest, InspectorFindPropertyResponse>
    {
        protected override async ETTask<IResponse> Run(InspectorFindPropertyRequest command)
        {
            await ETTask.CompletedTask;

            InspectorFindPropertyResponse response = InspectorFindPropertyResponse.Create();
            if (string.IsNullOrWhiteSpace(command.Keyword))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "Keyword is required";
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
                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = true;
                    if (iterator.propertyPath.IndexOf(command.Keyword, StringComparison.OrdinalIgnoreCase) < 0 &&
                        iterator.name.IndexOf(command.Keyword, StringComparison.OrdinalIgnoreCase) < 0 &&
                        iterator.displayName.IndexOf(command.Keyword, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    response.Properties.Add(CreatePropertyInfo(iterator));
                }

                response.TargetName = serializedTarget.name;
                response.TargetType = serializedTarget.GetType().Name;
                response.ComponentName = component == null ? string.Empty : component.GetType().Name;
                response.Keyword = command.Keyword;
                response.Count = response.Properties.Count;
                response.AssetPath = context.AssetPath;
                response.ObjectPath = context.ObjectPath;
                return response;
            }
        }
    }
}
