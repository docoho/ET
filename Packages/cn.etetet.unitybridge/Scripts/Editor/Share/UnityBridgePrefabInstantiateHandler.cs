using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 将指定路径 Prefab 资产实例化到当前场景（PrefabUtility.InstantiatePrefab，支持 Undo）的命令处理器（PrefabInstantiate）。
    /// </summary>
    internal sealed class UnityBridgePrefabInstantiateHandler : AUnityBridgeHandler<PrefabInstantiateRequest, PrefabInstantiateResponse>
    {
        protected override async ETTask<IResponse> Run(PrefabInstantiateRequest command)
        {
            await ETTask.CompletedTask;

            PrefabInstantiateResponse response = PrefabInstantiateResponse.Create();
            response.PrefabPath = command.PrefabPath;
            if (string.IsNullOrWhiteSpace(command.PrefabPath))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "PrefabPath is required";
                return response;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(command.PrefabPath);
            if (prefab == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = $"prefab not found: {command.PrefabPath}";
                return response;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = $"failed to instantiate prefab: {command.PrefabPath}";
                return response;
            }

            if (command.Position != null)
            {
                instance.transform.position = new Vector3(command.Position.X, command.Position.Y, command.Position.Z);
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {prefab.name}");
            Selection.activeGameObject = instance;
            response.Instance = CreateObjectInfo(instance);
            return response;
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
            info.LayerName = LayerMask.LayerToName(gameObject.layer);
            info.Layer = gameObject.layer;
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
