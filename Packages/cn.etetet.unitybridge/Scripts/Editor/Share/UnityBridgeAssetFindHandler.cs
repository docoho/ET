using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 资源查找命令处理器（AssetFind），基于 AssetDatabase.FindAssets 按过滤条件与目录列表检索工程资源。
    /// </summary>
    internal sealed class UnityBridgeAssetFindHandler : AUnityBridgeHandler<AssetFindRequest, AssetFindResponse>
    {
        protected override async ETTask<IResponse> Run(AssetFindRequest command)
        {
            await ETTask.CompletedTask;

            string filter = command.Filter ?? string.Empty;
            string format = NormalizeFormat(command.Format);
            string[] folders = NormalizeFolders(command.SearchInFolders);
            string[] guids = folders.Length > 0
                    ? AssetDatabase.FindAssets(filter, folders)
                    : AssetDatabase.FindAssets(filter);
            int count = Math.Min(guids.Length, NormalizeMaxResults(command.MaxResults));

            AssetFindResponse response = AssetFindResponse.Create();
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
