using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET
{
    /// <summary>
    /// UnityBridge 通过反射访问 UnityEditor.LogEntries 读取 Unity 控制台日志列表的命令处理器（ConsoleGetLogs）。
    /// </summary>
    internal sealed class UnityBridgeConsoleGetLogsHandler : AUnityBridgeHandler<ConsoleGetLogsRequest, ConsoleGetLogsResponse>
    {
        protected override async ETTask<IResponse> Run(ConsoleGetLogsRequest command)
        {
            await ETTask.CompletedTask;

            string logType = string.IsNullOrWhiteSpace(command.LogType) ? "all" : command.LogType.Trim();
            int maxCount = command.Count > 0 ? command.Count : 50;
            List<BridgeConsoleLog> logs = GetConsoleLogs(maxCount, logType, out int totalCount);

            ConsoleGetLogsResponse response = ConsoleGetLogsResponse.Create();
            response.LogType = logType;
            response.TotalCount = totalCount;
            response.Count = logs.Count;
            foreach (BridgeConsoleLog log in logs)
            {
                response.Logs.Add(log);
            }

            return response;
        }

        private static List<BridgeConsoleLog> GetConsoleLogs(int maxCount, string logTypeFilter, out int totalCount)
        {
            totalCount = 0;
            List<BridgeConsoleLog> logs = new();

            Type logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            Type logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
            if (logEntriesType == null || logEntryType == null)
            {
                return logs;
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            MethodInfo getCountMethod = logEntriesType.GetMethod("GetCount", flags);
            MethodInfo startMethod = logEntriesType.GetMethod("StartGettingEntries", flags);
            MethodInfo endMethod = logEntriesType.GetMethod("EndGettingEntries", flags);
            MethodInfo getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", flags);
            if (getCountMethod == null || startMethod == null || endMethod == null || getEntryMethod == null)
            {
                return logs;
            }

            totalCount = (int)(getCountMethod.Invoke(null, null) ?? 0);
            if (totalCount <= 0)
            {
                return logs;
            }

            startMethod.Invoke(null, null);
            try
            {
                int startIndex = Math.Max(0, totalCount - maxCount);
                for (int i = startIndex; i < totalCount; ++i)
                {
                    object entry = Activator.CreateInstance(logEntryType);
                    bool success = (bool)(getEntryMethod.Invoke(null, new[] { i, entry }) ?? false);
                    if (!success)
                    {
                        continue;
                    }

                    string entryType = GetLogType(GetIntField(logEntryType, entry, "mode"));
                    if (!MatchesLogType(logTypeFilter, entryType))
                    {
                        continue;
                    }

                    BridgeConsoleLog log = BridgeConsoleLog.Create();
                    log.LogType = entryType;
                    log.Message = GetStringField(logEntryType, entry, "message");
                    log.StackTrace = GetStringField(logEntryType, entry, "stackTrace");
                    log.Time = GetStringField(logEntryType, entry, "time");
                    logs.Add(log);
                }
            }
            finally
            {
                endMethod.Invoke(null, null);
            }

            return logs;
        }

        private static bool MatchesLogType(string filter, string entryType)
        {
            return string.IsNullOrWhiteSpace(filter) ||
                    string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(filter, entryType, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetIntField(Type type, object instance, string fieldName)
        {
            object value = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return value is int intValue ? intValue : 0;
        }

        private static string GetStringField(Type type, object instance, string fieldName)
        {
            object value = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return value?.ToString() ?? string.Empty;
        }

        private static string GetLogType(int mode)
        {
            if ((mode & (1 << 0)) != 0)
            {
                return "Error";
            }

            if ((mode & (1 << 1)) != 0)
            {
                return "Assert";
            }

            if ((mode & (1 << 2)) != 0)
            {
                return "Log";
            }

            if ((mode & (1 << 3)) != 0)
            {
                return "Fatal";
            }

            if ((mode & (1 << 7)) != 0)
            {
                return "ScriptingError";
            }

            if ((mode & (1 << 8)) != 0)
            {
                return "ScriptingWarning";
            }

            if ((mode & (1 << 9)) != 0)
            {
                return "ScriptingLog";
            }

            if ((mode & (1 << 11)) != 0)
            {
                return "Warning";
            }

            return "Log";
        }
    }
}
