using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 根据资源 GUID 查询工程路径及元数据的命令处理器（AssetGetPath）。
    /// </summary>
    internal sealed class UnityBridgeAssetGetPathHandler : AUnityBridgeHandler<AssetGetPathRequest, AssetGetPathResponse>
    {
        protected override async ETTask<IResponse> Run(AssetGetPathRequest command)
        {
            await ETTask.CompletedTask;
            if (string.IsNullOrWhiteSpace(command.Guid))
            {
                throw new ArgumentException("asset guid is required");
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(command.Guid.Trim());
            AssetGetPathResponse response = AssetGetPathResponse.Create();
            response.Guid = command.Guid.Trim();
            response.AssetPath = assetPath;
            response.Exists = !string.IsNullOrWhiteSpace(assetPath);
            if (response.Exists)
            {
                response.Asset = CreateAssetInfo(assetPath, response.Guid);
            }

            return response;
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
