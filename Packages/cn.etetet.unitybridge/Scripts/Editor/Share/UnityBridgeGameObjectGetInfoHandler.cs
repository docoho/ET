using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 获取场景中指定 GameObject 的详细属性、Transform、组件列表及直接子节点名称的命令处理器（GameObjectGetInfo）。
    /// </summary>
    internal sealed class UnityBridgeGameObjectGetInfoHandler : AUnityBridgeHandler<GameObjectGetInfoRequest, GameObjectGetInfoResponse>
    {
        protected override async ETTask<IResponse> Run(GameObjectGetInfoRequest command)
        {
            await ETTask.CompletedTask;

            GameObjectGetInfoResponse response = GameObjectGetInfoResponse.Create();
            GameObject gameObject = FindGameObject(command.Path, command.InstanceId);
            if (gameObject == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameObject not found";
                return response;
            }

            int maxChildren = command.MaxChildren > 0 ? command.MaxChildren : 20;
            response.Object = CreateObjectInfo(gameObject, command.IncludeComponents);
            response.ChildCount = gameObject.transform.childCount;
            response.ParentName = gameObject.transform.parent == null ? string.Empty : gameObject.transform.parent.name;
            for (int i = 0; i < gameObject.transform.childCount && i < maxChildren; ++i)
            {
                response.Children.Add(gameObject.transform.GetChild(i).name);
            }

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

        private static BridgeObjectInfo CreateObjectInfo(GameObject gameObject, bool includeComponents)
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
            info.Transform = CreateTransformInfo(gameObject.transform);
            if (includeComponents)
            {
                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; ++i)
                {
                    if (components[i] != null)
                    {
                        info.Components.Add(CreateComponentInfo(components[i], i));
                    }
                }
            }

            return info;
        }

        private static BridgeTransformInfo CreateTransformInfo(Transform transform)
        {
            BridgeTransformInfo info = BridgeTransformInfo.Create();
            info.LocalPosition = CreateVector3(transform.localPosition);
            info.LocalEulerAngles = CreateVector3(transform.localEulerAngles);
            info.LocalRotation = CreateQuaternion(transform.localRotation);
            info.LocalScale = CreateVector3(transform.localScale);
            info.ParentPath = transform.parent == null ? string.Empty : GetPath(transform.parent.gameObject);
            info.SiblingIndex = transform.GetSiblingIndex();
            info.Position = CreateVector3(transform.position);
            info.EulerAngles = CreateVector3(transform.eulerAngles);
            info.ChildCount = transform.childCount;
            return info;
        }

        private static BridgeComponentInfo CreateComponentInfo(Component component, int index)
        {
            BridgeComponentInfo info = BridgeComponentInfo.Create();
            info.TypeName = component.GetType().Name;
            info.FullTypeName = component.GetType().FullName;
            info.ComponentIndex = index;
            info.InstanceId = component.GetInstanceID();
            info.Enabled = component is Behaviour behaviour ? behaviour.enabled : true;
            return info;
        }

        private static BridgeVector3 CreateVector3(Vector3 value)
        {
            BridgeVector3 result = BridgeVector3.Create();
            result.X = value.x;
            result.Y = value.y;
            result.Z = value.z;
            return result;
        }

        private static BridgeQuaternion CreateQuaternion(Quaternion value)
        {
            BridgeQuaternion result = BridgeQuaternion.Create();
            result.X = value.x;
            result.Y = value.y;
            result.Z = value.z;
            result.W = value.w;
            return result;
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
