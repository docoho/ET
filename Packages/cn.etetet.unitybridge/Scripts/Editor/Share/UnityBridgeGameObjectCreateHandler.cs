using System;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 在当前场景中创建空 GameObject 或基础几何体（Primitive）并注册 Undo 的命令处理器（GameObjectCreate）。
    /// </summary>
    internal sealed class UnityBridgeGameObjectCreateHandler : AUnityBridgeHandler<GameObjectCreateRequest, GameObjectCreateResponse>
    {
        protected override async ETTask<IResponse> Run(GameObjectCreateRequest command)
        {
            await ETTask.CompletedTask;

            GameObjectCreateResponse response = GameObjectCreateResponse.Create();
            string name = string.IsNullOrWhiteSpace(command.Name) ? "New GameObject" : command.Name;
            GameObject gameObject;

            if (string.IsNullOrWhiteSpace(command.PrimitiveType))
            {
                gameObject = new GameObject(name);
            }
            else if (Enum.TryParse(command.PrimitiveType, true, out PrimitiveType primitiveType))
            {
                gameObject = GameObject.CreatePrimitive(primitiveType);
                gameObject.name = name;
            }
            else
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = $"unknown primitive type: {command.PrimitiveType}";
                return response;
            }

            if (!string.IsNullOrWhiteSpace(command.ParentPath))
            {
                GameObject parent = FindGameObject(command.ParentPath, 0);
                if (parent == null)
                {
                    Undo.DestroyObjectImmediate(gameObject);
                    response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                    response.Message = $"parent GameObject not found: {command.ParentPath}";
                    return response;
                }

                gameObject.transform.SetParent(parent.transform, false);
            }

            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            Selection.activeGameObject = gameObject;

            response.Created = true;
            response.Object = CreateObjectInfo(gameObject, false);
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
