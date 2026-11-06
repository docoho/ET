using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 克隆复制场景中指定 GameObject（支持 Undo）的命令处理器（GameObjectDuplicate）。
    /// </summary>
    internal sealed class UnityBridgeGameObjectDuplicateHandler : AUnityBridgeHandler<GameObjectDuplicateRequest, GameObjectDuplicateResponse>
    {
        protected override async ETTask<IResponse> Run(GameObjectDuplicateRequest command)
        {
            await ETTask.CompletedTask;

            GameObjectDuplicateResponse response = GameObjectDuplicateResponse.Create();
            GameObject gameObject = FindGameObject(command.Path, command.InstanceId);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameObject not found";
                return response;
            }

            GameObject duplicate = UnityEngine.Object.Instantiate(gameObject, gameObject.transform.parent);
            duplicate.name = gameObject.name;
            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {gameObject.name}");
            Selection.activeGameObject = duplicate;

            response.Original = CreateObjectInfo(gameObject);
            response.Duplicate = CreateObjectInfo(duplicate);
            return response;
        }

        private static GameObject FindGameObject(string path, int instanceId)
        {
            if (instanceId != 0)
            {
#if UNITY_6000_3_OR_NEWER
                return EditorUtility.EntityIdToObject(instanceId) as GameObject;
#else
                return EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#endif
            }

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
            info.Tag = gameObject.tag;
            info.Layer = gameObject.layer;
            info.LayerName = LayerMask.LayerToName(gameObject.layer);
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
