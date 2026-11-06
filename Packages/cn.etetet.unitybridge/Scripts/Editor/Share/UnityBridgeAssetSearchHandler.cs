using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 按预设资源类型模式（prefab/scene/script/texture/so 等）与关键字搜索工程资源的命令处理器（AssetSearch）。
    /// </summary>
    internal sealed class UnityBridgeAssetSearchHandler : AUnityBridgeHandler<AssetSearchRequest, AssetSearchResponse>
    {
        protected override async ETTask<IResponse> Run(AssetSearchRequest command)
        {
            await ETTask.CompletedTask;

            string mode = string.IsNullOrWhiteSpace(command.Mode) ? "all" : command.Mode.Trim();
            string filter = BuildFilter(mode, command.Filter, command.Keyword);
            string format = NormalizeFormat(command.Format);
            string[] folders = NormalizeFolders(command.SearchInFolders);
            string[] guids = folders.Length > 0
                    ? AssetDatabase.FindAssets(filter, folders)
                    : AssetDatabase.FindAssets(filter);
            int count = Math.Min(guids.Length, NormalizeMaxResults(command.MaxResults));

            AssetSearchResponse response = AssetSearchResponse.Create();
            response.Mode = mode;
            response.Filter = filter;
            response.TotalFound = guids.Length;
            response.Returned = count;
            for (int i = 0; i < count; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                response.Paths.Add(path);
                if (format == "full")
                {
                    response.Assets.Add(CreateAssetInfo(path, guids[i]));
                }
            }

            return response;
        }

        private static string BuildFilter(string mode, string customFilter, string keyword)
        {
            string filter;
            if (!string.IsNullOrWhiteSpace(customFilter))
            {
                filter = customFilter.Trim();
            }
            else
            {
                filter = GetModeFilter(mode);
            }

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return filter;
            }

            string trimmedKeyword = keyword.Trim();
            return string.IsNullOrWhiteSpace(filter) ? trimmedKeyword : $"{filter} {trimmedKeyword}";
        }

        private static string GetModeFilter(string mode)
        {
            switch (mode.Trim().ToLowerInvariant())
            {
                case "all":
                    return string.Empty;
                case "prefab":
                    return "t:Prefab";
                case "scene":
                    return "t:Scene";
                case "script":
                    return "t:Script";
                case "texture":
                    return "t:Texture";
                case "material":
                    return "t:Material";
                case "audio":
                    return "t:AudioClip";
                case "animation":
                    return "t:AnimationClip";
                case "shader":
                    return "t:Shader";
                case "font":
                    return "t:Font";
                case "model":
                    return "t:Model";
                case "so":
                    return "t:ScriptableObject";
                default:
                    throw new ArgumentException($"unknown asset search mode: {mode}");
            }
        }

        private static string NormalizeFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return "full";
            }

            string trimmed = format.Trim().ToLowerInvariant();
            if (trimmed == "full" || trimmed == "paths")
            {
                return trimmed;
            }

            throw new ArgumentException($"invalid asset response format: {format}");
        }

        private static int NormalizeMaxResults(int maxResults)
        {
            return maxResults > 0 ? maxResults : 100;
        }

        private static string[] NormalizeFolders(IEnumerable<string> folders)
        {
            if (folders == null)
            {
                return Array.Empty<string>();
            }

            List<string> normalized = new();
            foreach (string folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                normalized.Add(folder.Trim());
            }

            return normalized.ToArray();
        }

        private static BridgeAssetInfo CreateAssetInfo(string assetPath, string guid)
        {
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            BridgeAssetInfo info = BridgeAssetInfo.Create();
            info.AssetPath = assetPath;
            info.Guid = string.IsNullOrWhiteSpace(guid) ? AssetDatabase.AssetPathToGUID(assetPath) : guid;
            info.TypeName = assetType?.Name ?? "Unknown";
            info.Name = Path.GetFileNameWithoutExtension(assetPath);
            info.Extension = Path.GetExtension(assetPath);
            info.FileSize = GetFileSize(assetPath);
            return info;
        }

        private static long GetFileSize(string assetPath)
        {
            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrWhiteSpace(projectRoot) || string.IsNullOrWhiteSpace(assetPath))
                {
                    return 0;
                }

                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('\\', '/')));
                return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
