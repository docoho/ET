using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 按名称、Tag、挂载组件类型及激活状态检索场景中 GameObject 列表的命令处理器（GameObjectFind）。
    /// </summary>
    internal sealed class UnityBridgeGameObjectFindHandler : AUnityBridgeHandler<GameObjectFindRequest, GameObjectFindResponse>
    {
        protected override async ETTask<IResponse> Run(GameObjectFindRequest command)
        {
            await ETTask.CompletedTask;

            GameObjectFindResponse response = GameObjectFindResponse.Create();
            int maxResults = command.MaxResults > 0 ? command.MaxResults : 50;
            foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (gameObject == null || EditorUtility.IsPersistent(gameObject) || !gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!command.IncludeInactive && !gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(command.Name) && !gameObject.name.Contains(command.Name))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(command.Tag) && gameObject.tag != command.Tag)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(command.WithComponent) && !HasComponent(gameObject, command.WithComponent))
                {
                    continue;
                }

                response.Objects.Add(CreateObjectInfo(gameObject, command.IncludeComponents));
                if (response.Objects.Count >= maxResults)
                {
                    break;
                }
            }

            response.Count = response.Objects.Count;
            return response;
        }

        private static bool HasComponent(GameObject gameObject, string componentName)
        {
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component != null && (component.GetType().Name == componentName || component.GetType().FullName == componentName))
                {
                    return true;
                }
            }

            return false;
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
