using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 解包场景中的 Prefab 实例（支持最外层或完全解包）的命令处理器（PrefabUnpack）。
    /// </summary>
    internal sealed class UnityBridgePrefabUnpackHandler : AUnityBridgeHandler<PrefabUnpackRequest, PrefabUnpackResponse>
    {
        protected override async ETTask<IResponse> Run(PrefabUnpackRequest command)
        {
            await ETTask.CompletedTask;

            PrefabUnpackResponse response = PrefabUnpackResponse.Create();
            GameObject gameObject = FindGameObject(command.GameObjectPath);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameObject not found";
                return response;
            }

            if (!PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = $"GameObject is not part of a prefab: {gameObject.name}";
                return response;
            }

            PrefabUnpackMode mode = command.Completely ? PrefabUnpackMode.Completely : PrefabUnpackMode.OutermostRoot;
            PrefabUtility.UnpackPrefabInstance(gameObject, mode, InteractionMode.AutomatedAction);

            response.GameObjectName = gameObject.name;
            response.Unpacked = true;
            response.Completely = command.Completely;
            response.Object = CreateObjectInfo(gameObject);
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

        private static BridgeObjectInfo CreateObjectInfo(GameObject gameObject)
        {
            BridgeObjectInfo info = BridgeObjectInfo.Create();
            info.Name = gameObject.name;
            info.Path = GetPath(gameObject);
            info.InstanceId = gameObject.GetInstanceID();
            info.ActiveSelf = gameObject.activeSelf;
            info.ActiveInHierarchy = gameObject.activeInHierarchy;
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
