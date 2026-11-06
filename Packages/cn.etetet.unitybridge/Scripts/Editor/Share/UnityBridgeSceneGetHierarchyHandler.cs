using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace ET
{
    /// <summary>
    /// UnityBridge 获取当前激活场景中所有根 GameObject 及其指定深度子树层级与组件信息的命令处理器（SceneGetHierarchy）。
    /// </summary>
    internal sealed class UnityBridgeSceneGetHierarchyHandler : AUnityBridgeHandler<SceneGetHierarchyRequest, SceneGetHierarchyResponse>
    {
        protected override async ETTask<IResponse> Run(SceneGetHierarchyRequest command)
        {
            await ETTask.CompletedTask;

            UnityScene scene = SceneManager.GetActiveScene();
            int depth = command.Depth > 0 ? command.Depth : 3;

            SceneGetHierarchyResponse response = SceneGetHierarchyResponse.Create();
            response.SceneName = scene.name;
            response.ScenePath = scene.path;

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                if (!command.IncludeInactive && !root.activeInHierarchy)
                {
                    continue;
                }

                response.Roots.Add(CreateSceneNode(root, depth, command.IncludeInactive));
            }

            response.RootCount = response.Roots.Count;
            return response;
        }

        private static BridgeSceneNode CreateSceneNode(GameObject gameObject, int remainingDepth, bool includeInactive)
        {
            BridgeSceneNode node = BridgeSceneNode.Create();
            node.Object = CreateObjectInfo(gameObject);

            if (remainingDepth <= 0)
            {
                return node;
            }

            foreach (Transform child in gameObject.transform)
            {
                if (!includeInactive && !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                node.Children.Add(CreateSceneNode(child.gameObject, remainingDepth - 1, includeInactive));
            }

            return node;
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
            info.Transform = CreateTransformInfo(gameObject.transform);

            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; ++i)
            {
                if (components[i] == null)
                {
                    continue;
                }

                info.Components.Add(CreateComponentInfo(components[i], i));
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
            return info;
        }

        private static BridgeComponentInfo CreateComponentInfo(Component component, int index)
        {
            BridgeComponentInfo info = BridgeComponentInfo.Create();
            info.TypeName = component.GetType().Name;
            info.FullTypeName = component.GetType().FullName;
            info.ComponentIndex = index;
            info.InstanceId = component.GetInstanceID();
            info.Enabled = IsComponentEnabled(component);
            return info;
        }

        private static bool IsComponentEnabled(Component component)
        {
            return component switch
            {
                Collider2D collider2D => collider2D.enabled,
                Behaviour behaviour => behaviour.enabled,
                Renderer renderer => renderer.enabled,
                Collider collider => collider.enabled,
                _ => true
            };
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
