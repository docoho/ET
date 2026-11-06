using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 读取工程内文本资源内容（支持指定起始行、最大行数与字符截断保护）的命令处理器（AssetReadText）。
    /// </summary>
    internal sealed class UnityBridgeAssetReadTextHandler : AUnityBridgeHandler<AssetReadTextRequest, AssetReadTextResponse>
    {
        protected override async ETTask<IResponse> Run(AssetReadTextRequest command)
        {
            await ETTask.CompletedTask;
            if (string.IsNullOrWhiteSpace(command.AssetPath))
            {
                throw new ArgumentException("asset path is required");
            }

            string normalizedAssetPath = command.AssetPath.Replace('\\', '/').Trim();
            string fullPath = ResolveProjectPath(normalizedAssetPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"text asset not found at path: {normalizedAssetPath}", fullPath);
            }

            string text = File.ReadAllText(fullPath, Encoding.UTF8);
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            return BuildResponse(command, normalizedAssetPath, lines);
        }

        private static string ResolveProjectPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("unity project root cannot be resolved");
            }

            string fullProjectRoot = Path.GetFullPath(projectRoot + Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            if (!fullPath.StartsWith(fullProjectRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"asset path escapes project root: {assetPath}");
            }

            return fullPath;
        }

        private static AssetReadTextResponse BuildResponse(AssetReadTextRequest command, string assetPath, string[] lines)
        {
            int startLine = command.StartLine > 0 ? command.StartLine : 1;
            int maxLines = command.MaxLines > 0 ? command.MaxLines : 200;
            int maxChars = command.MaxChars > 0 ? Math.Max(200, command.MaxChars) : 12000;
            int startIndex = startLine - 1;

            AssetReadTextResponse response = AssetReadTextResponse.Create();
            response.AssetPath = assetPath;
            response.TotalLines = lines.Length;
            response.ReturnedLineStart = startLine;

            if (startIndex >= lines.Length)
            {
                response.ReturnedLineEnd = startLine - 1;
                response.ReturnedLineCount = 0;
                response.Truncated = false;
                response.Content = string.Empty;
                return response;
            }

            StringBuilder builder = new();
            int returnedLines = 0;
            bool truncated = false;
            for (int i = startIndex; i < lines.Length; ++i)
            {
                int currentLine = i + 1;
                string lineWithNumber = $"{currentLine}: {lines[i]}";
                int separatorLength = returnedLines > 0 ? 1 : 0;
                if (builder.Length + separatorLength + lineWithNumber.Length > maxChars)
                {
                    truncated = true;
                    break;
                }

                if (returnedLines > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(lineWithNumber);
                ++returnedLines;
                if (returnedLines >= maxLines)
                {
                    truncated = i < lines.Length - 1;
                    break;
                }
            }

            response.ReturnedLineEnd = returnedLines == 0 ? startLine - 1 : startLine + returnedLines - 1;
            response.ReturnedLineCount = returnedLines;
            response.Truncated = truncated;
            response.Content = builder.ToString();
            return response;
        }
    }
}
