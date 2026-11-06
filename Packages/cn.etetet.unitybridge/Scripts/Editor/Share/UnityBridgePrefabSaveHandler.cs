using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 将场景中的 GameObject 保存为指定路径的 Prefab 资产（PrefabUtility.SaveAsPrefabAsset）的命令处理器（PrefabSave）。
    /// </summary>
    internal sealed class UnityBridgePrefabSaveHandler : AUnityBridgeHandler<PrefabSaveRequest, PrefabSaveResponse>
    {
        protected override async ETTask<IResponse> Run(PrefabSaveRequest command)
        {
            await ETTask.CompletedTask;

            PrefabSaveResponse response = PrefabSaveResponse.Create();
            if (string.IsNullOrWhiteSpace(command.SavePath))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "SavePath is required";
                return response;
            }

            GameObject gameObject = FindGameObject(command.GameObjectPath);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameObject not found";
                return response;
            }

            string savePath = command.SavePath.EndsWith(".prefab") ? command.SavePath : command.SavePath + ".prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(gameObject, savePath);
            AssetDatabase.Refresh();

            response.GameObjectName = gameObject.name;
            response.PrefabPath = savePath;
            response.Saved = savedPrefab != null;
            if (savedPrefab == null)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = $"failed to save prefab: {savePath}";
                return response;
            }

            response.Asset = CreateAssetInfo(savedPrefab, savePath);
            return response;
        }

        private static GameObject FindGameObject(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Selection.activeGameObject;
            }

            GameObject gameObject = GameObject.Find(path);
            if (gameObject != null)
            {
                return gameObject;
            }

            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate == null || EditorUtility.IsPersistent(candidate) || !candidate.scene.IsValid())
                {
                    continue;
                }

                if (GetPath(candidate) == path)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static BridgeAssetInfo CreateAssetInfo(UnityEngine.Object asset, string assetPath)
        {
            BridgeAssetInfo info = BridgeAssetInfo.Create();
            info.AssetPath = assetPath;
            info.Guid = AssetDatabase.AssetPathToGUID(assetPath);
            info.TypeName = asset.GetType().Name;
            info.Name = asset.name;
            info.Extension = Path.GetExtension(assetPath);
            info.InstanceId = asset.GetInstanceID();
            return info;
        }

        private static string GetPath(GameObject gameObject)
        {
            string path = gameObject.name;
            Transform parent = gameObject.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
