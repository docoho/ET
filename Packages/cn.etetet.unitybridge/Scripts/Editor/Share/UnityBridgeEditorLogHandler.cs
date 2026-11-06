using System;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 向 Unity 编辑器控制台输出指定级别（Log / Warning / Error）日志的命令处理器（EditorLog）。
    /// </summary>
    internal sealed class UnityBridgeEditorLogHandler : AUnityBridgeHandler<EditorLogRequest, EditorLogResponse>
    {
        protected override async ETTask<IResponse> Run(EditorLogRequest command)
        {
            await ETTask.CompletedTask;
            if (string.IsNullOrWhiteSpace(command.Message))
            {
                throw new ArgumentException("editor log message is required");
            }

            string logType = NormalizeLogType(command.LogType);
            string loggedMessage = "[UnityBridge] " + command.Message;
            switch (logType)
            {
                case "Error":
                    Debug.LogError(loggedMessage);
                    break;
                case "Warning":
                    Debug.LogWarning(loggedMessage);
                    break;
                default:
                    Debug.Log(loggedMessage);
                    break;
            }

            EditorLogResponse response = EditorLogResponse.Create();
            response.Logged = true;
            response.LogType = logType;
            response.LoggedMessage = loggedMessage;
            return response;
        }

        private static string NormalizeLogType(string logType)
        {
            if (string.IsNullOrWhiteSpace(logType))
            {
                return "Log";
            }

            switch (logType.Trim().ToLowerInvariant())
            {
                case "error":
                    return "Error";
                case "warning":
                    return "Warning";
                case "log":
                    return "Log";
                default:
                    throw new ArgumentException($"invalid editor log type: {logType}");
            }
        }
    }
}
