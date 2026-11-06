using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 销毁场景中指定路径或 InstanceId 的 GameObject（支持 Undo）的命令处理器（GameObjectDestroy）。
    /// </summary>
    internal sealed class UnityBridgeGameObjectDestroyHandler : AUnityBridgeHandler<GameObjectDestroyRequest, GameObjectDestroyResponse>
    {
        protected override async ETTask<IResponse> Run(GameObjectDestroyRequest command)
        {
            await ETTask.CompletedTask;

            GameObjectDestroyResponse response = GameObjectDestroyResponse.Create();
            GameObject gameObject = FindGameObject(command.Path, command.InstanceId);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameObject not found";
                return response;
            }

            response.DestroyedName = gameObject.name;
            response.DestroyedPath = GetPath(gameObject);
            Undo.DestroyObjectImmediate(gameObject);
            response.Destroyed = true;
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
