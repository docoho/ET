using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 加载指定路径主资源并返回资源元数据与 InstanceId 的命令处理器（AssetLoad）。
    /// </summary>
    internal sealed class UnityBridgeAssetLoadHandler : AUnityBridgeHandler<AssetLoadRequest, AssetLoadResponse>
    {
        protected override async ETTask<IResponse> Run(AssetLoadRequest command)
        {
            await ETTask.CompletedTask;
            if (string.IsNullOrWhiteSpace(command.AssetPath))
            {
                throw new ArgumentException("asset path is required");
            }

            string assetPath = NormalizeAssetPath(command.AssetPath);
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            AssetLoadResponse response = AssetLoadResponse.Create();
            response.Exists = asset != null;
            if (asset == null)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = $"asset not found at path: {assetPath}";
                return response;
            }

            response.Asset = CreateAssetInfo(assetPath, AssetDatabase.AssetPathToGUID(assetPath));
            response.InstanceId = asset.GetInstanceID();
            return response;
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace('\\', '/').Trim();
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
