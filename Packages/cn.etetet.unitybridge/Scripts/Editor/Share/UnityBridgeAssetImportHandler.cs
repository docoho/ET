using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 导入指定路径资源的延迟命令处理器（AssetImport），支持跨脚本编译重导入并等待导入完成。
    /// </summary>
    internal sealed class UnityBridgeAssetImportHandler : AUnityBridgeDeferredHandler<AssetImportRequest, AssetImportResponse>
    {
        protected override async ETTask<AssetImportResponse> Run(AssetImportRequest command, UnityBridgeDeferredContext deferred)
        {
            await ETTask.CompletedTask;
            
            if (!deferred.IsResuming && EditorApplication.isCompiling)
            {
                throw new UnityBridgeCommandStateException(UnityBridgeErrorCode.Compiling, "unity is compiling");
            }

            if (string.IsNullOrWhiteSpace(command.AssetPath))
            {
                throw new ArgumentException("asset path is required");
            }

            string assetPath = NormalizeAssetPath(command.AssetPath);
            if (!deferred.IsResuming)
            {
                AssetDatabase.ImportAsset(assetPath, ToImportOptions(command.ForceUpdate));
                return deferred.Started<AssetImportResponse>();
            }

            if (EditorApplication.isCompiling)
            {
                return deferred.NotReady<AssetImportResponse>();
            }

            AssetImportResponse response = AssetImportResponse.Create();
            response.AssetPath = assetPath;
            response.Imported = true;
            if (EditorUtility.scriptCompilationFailed)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = "asset import finished with compile errors";
                return response;
            }

            string guid = AssetDatabase.AssetPathToGUID(response.AssetPath);
            if (!string.IsNullOrWhiteSpace(guid))
            {
                response.Asset = CreateAssetInfo(response.AssetPath, guid);
            }

            response.Message = "asset import completed";
            return response;
        }

        private static ImportAssetOptions ToImportOptions(bool forceUpdate)
        {
            return forceUpdate ? ImportAssetOptions.ForceUpdate : ImportAssetOptions.Default;
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
