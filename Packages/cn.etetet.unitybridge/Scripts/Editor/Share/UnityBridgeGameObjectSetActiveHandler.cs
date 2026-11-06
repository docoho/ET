using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 设置或切换场景中指定 GameObject 激活状态（SetActive，支持 Undo）的命令处理器（GameObjectSetActive）。
    /// </summary>
    internal sealed class UnityBridgeGameObjectSetActiveHandler : AUnityBridgeHandler<GameObjectSetActiveRequest, GameObjectSetActiveResponse>
    {
        protected override async ETTask<IResponse> Run(GameObjectSetActiveRequest command)
        {
            await ETTask.CompletedTask;

            GameObjectSetActiveResponse response = GameObjectSetActiveResponse.Create();
            GameObject gameObject = FindGameObject(command.Path, command.InstanceId);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameObject not found";
                return response;
            }

            bool active = command.Toggle ? !gameObject.activeSelf : command.Active;
            Undo.RecordObject(gameObject, $"Set Active {gameObject.name}");
            gameObject.SetActive(active);

            response.Object = CreateObjectInfo(gameObject);
            response.ActiveSelf = gameObject.activeSelf;
            response.ActiveInHierarchy = gameObject.activeInHierarchy;
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
