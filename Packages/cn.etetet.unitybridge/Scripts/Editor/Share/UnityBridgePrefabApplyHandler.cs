using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 将场景中 Prefab 实例的覆盖修改应用回源 Prefab 资产（PrefabUtility.ApplyPrefabInstance）的命令处理器（PrefabApply）。
    /// </summary>
    internal sealed class UnityBridgePrefabApplyHandler : AUnityBridgeHandler<PrefabApplyRequest, PrefabApplyResponse>
    {
        protected override async ETTask<IResponse> Run(PrefabApplyRequest command)
        {
            await ETTask.CompletedTask;

            PrefabApplyResponse response = PrefabApplyResponse.Create();
            GameObject gameObject = FindGameObject(command.GameObjectPath);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameObject not found";
                return response;
            }

            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = $"GameObject is not a prefab instance: {gameObject.name}";
                return response;
            }

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            PrefabUtility.ApplyPrefabInstance(gameObject, InteractionMode.AutomatedAction);
            AssetDatabase.Refresh();

            response.GameObjectName = gameObject.name;
            response.PrefabPath = prefabPath;
            response.Applied = true;
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
