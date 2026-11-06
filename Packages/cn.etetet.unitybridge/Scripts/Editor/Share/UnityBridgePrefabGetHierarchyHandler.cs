using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 获取指定路径 Prefab 资产内部节点层级树与组件信息的命令处理器（PrefabGetHierarchy）。
    /// </summary>
    internal sealed class UnityBridgePrefabGetHierarchyHandler : AUnityBridgeHandler<PrefabGetHierarchyRequest, PrefabGetHierarchyResponse>
    {
        protected override async ETTask<IResponse> Run(PrefabGetHierarchyRequest command)
        {
            await ETTask.CompletedTask;

            PrefabGetHierarchyResponse response = PrefabGetHierarchyResponse.Create();
            response.PrefabPath = command.PrefabPath;
            if (string.IsNullOrWhiteSpace(command.PrefabPath))
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "PrefabPath is required";
                return response;
            }

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(command.PrefabPath);
            if (prefabRoot == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = $"prefab not found: {command.PrefabPath}";
                return response;
            }

            int depth = command.Depth >= 0 ? command.Depth : 5;
            bool truncated = false;
            if (command.IncludeInactive || prefabRoot.activeSelf)
            {
                response.Roots.Add(BuildNode(prefabRoot, prefabRoot.name, depth, command.IncludeInactive, command.IncludeComponents, ref truncated));
            }

            response.PrefabName = prefabRoot.name;
            response.RootCount = response.Roots.Count;
            response.Truncated = truncated;
            return response;
        }

        private static BridgeSceneNode BuildNode(GameObject gameObject, string path, int depth, bool includeInactive, bool includeComponents, ref bool truncated)
        {
            BridgeSceneNode node = BridgeSceneNode.Create();
            node.Object = CreateObjectInfo(gameObject, path, includeComponents);

            if (depth <= 0)
            {
                truncated |= gameObject.transform.childCount > 0;
                return node;
            }

            foreach (Transform child in gameObject.transform)
            {
                if (!includeInactive && !child.gameObject.activeSelf)
                {
                    continue;
                }

                node.Children.Add(BuildNode(child.gameObject, path + "/" + child.gameObject.name, depth - 1, includeInactive, includeComponents, ref truncated));
            }

            return node;
        }

        private static BridgeObjectInfo CreateObjectInfo(GameObject gameObject, string path, bool includeComponents)
        {
            BridgeObjectInfo info = BridgeObjectInfo.Create();
            info.Name = gameObject.name;
            info.Path = path;
            info.InstanceId = gameObject.GetInstanceID();
            info.ActiveSelf = gameObject.activeSelf;
            info.ActiveInHierarchy = gameObject.activeInHierarchy;
            info.Tag = gameObject.tag;
            info.LayerName = LayerMask.LayerToName(gameObject.layer);
            info.Layer = gameObject.layer;
            if (includeComponents)
            {
                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; ++i)
                {
                    if (components[i] == null)
                    {
                        continue;
                    }

                    BridgeComponentInfo componentInfo = BridgeComponentInfo.Create();
                    componentInfo.TypeName = components[i].GetType().Name;
                    componentInfo.FullTypeName = components[i].GetType().FullName;
                    componentInfo.ComponentIndex = i;
                    componentInfo.InstanceId = components[i].GetInstanceID();
                    componentInfo.Enabled = components[i] is Behaviour behaviour ? behaviour.enabled : true;
                    info.Components.Add(componentInfo);
                }
            }

            return info;
        }
    }
}
