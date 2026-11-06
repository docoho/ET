using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 查询指定 Prefab 资产或场景中 Prefab 实例的类型、状态及关联源资产路径的命令处理器（PrefabGetInfo）。
    /// </summary>
    internal sealed class UnityBridgePrefabGetInfoHandler : AUnityBridgeHandler<PrefabGetInfoRequest, PrefabGetInfoResponse>
    {
        protected override async ETTask<IResponse> Run(PrefabGetInfoRequest command)
        {
            await ETTask.CompletedTask;

            PrefabGetInfoResponse response = PrefabGetInfoResponse.Create();
            GameObject target = ResolveTarget(command, response);
            if (target == null)
            {
                return response;
            }

            response.Name = target.name;
            response.IsPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(target);
            response.IsPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(target);
            response.PrefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            response.PrefabType = PrefabUtility.GetPrefabAssetType(target).ToString();
            response.PrefabStatus = PrefabUtility.GetPrefabInstanceStatus(target).ToString();
            return response;
        }

        private static GameObject ResolveTarget(PrefabGetInfoRequest command, PrefabGetInfoResponse response)
        {
            if (!string.IsNullOrWhiteSpace(command.PrefabPath))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(command.PrefabPath);
                if (prefab == null)
                {
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = $"prefab not found: {command.PrefabPath}";
                }

                return prefab;
            }

            GameObject gameObject = FindGameObject(command.GameObjectPath);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "target GameObject not found";
            }

            return gameObject;
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
