using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace ET
{
    /// <summary>
    /// UnityBridge 设置当前编辑器选中集合（Selection.objects / activeObject）的命令处理器（SelectionSet）。
    /// </summary>
    internal sealed class UnityBridgeSelectionSetHandler : AUnityBridgeHandler<SelectionSetRequest, SelectionSetResponse>
    {
        protected override async ETTask<IResponse> Run(SelectionSetRequest command)
        {
            await ETTask.CompletedTask;

            List<UnityObject> selectedObjects = ResolveSelection(command);
            if (selectedObjects.Count == 0)
            {
                throw new ArgumentException("no objects found to select");
            }

            Selection.objects = selectedObjects.ToArray();
            Selection.activeObject = selectedObjects[0];

            SelectionSetResponse response = SelectionSetResponse.Create();
            response.SelectedCount = selectedObjects.Count;
            response.ActiveObjectName = Selection.activeObject == null ? string.Empty : Selection.activeObject.name;
            AppendSelection(response.Objects, response.Assets);
            return response;
        }

        private static List<UnityObject> ResolveSelection(SelectionSetRequest command)
        {
            List<UnityObject> selectedObjects = new();
            if (command.InstanceId != 0)
            {
                UnityObject obj = GetObjectByInstanceId(command.InstanceId);
                if (obj != null)
                {
                    selectedObjects.Add(obj);
                }

                return selectedObjects;
            }

            if (command.InstanceIds.Count > 0)
            {
                foreach (int instanceId in command.InstanceIds)
                {
                    UnityObject obj = GetObjectByInstanceId(instanceId);
                    if (obj != null)
                    {
                        selectedObjects.Add(obj);
                    }
                }

                return selectedObjects;
            }

            if (!string.IsNullOrWhiteSpace(command.Path))
            {
                GameObject gameObject = FindGameObjectByPath(command.Path);
                if (gameObject != null)
                {
                    selectedObjects.Add(gameObject);
                }

                return selectedObjects;
            }

            if (!string.IsNullOrWhiteSpace(command.AssetPath))
            {
                UnityObject asset = AssetDatabase.LoadMainAssetAtPath(command.AssetPath.Trim());
                if (asset != null)
                {
                    selectedObjects.Add(asset);
                }
            }

            return selectedObjects;
        }

        private static UnityObject GetObjectByInstanceId(int instanceId)
        {
#if UNITY_6000_3_OR_NEWER
            return EditorUtility.EntityIdToObject(instanceId);
#else
            return EditorUtility.InstanceIDToObject(instanceId);
#endif
        }

        private static GameObject FindGameObjectByPath(string path)
        {
            string normalizedPath = path.Trim().Trim('/');
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; ++sceneIndex)
            {
                UnityScene scene = SceneManager.GetSceneAt(sceneIndex);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    GameObject found = FindInRoot(root, normalizedPath);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        private static GameObject FindInRoot(GameObject root, string path)
        {
            if (path == root.name)
            {
                return root;
            }

            string prefix = root.name + "/";
            if (!path.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            string[] parts = path.Substring(prefix.Length).Split('/');
            Transform current = root.transform;
            foreach (string part in parts)
            {
                current = current.Find(part);
                if (current == null)
                {
                    return null;
                }
            }

            return current.gameObject;
        }

        private static void AppendSelection(List<BridgeObjectInfo> objects, List<BridgeAssetInfo> assets)
        {
            foreach (UnityObject selected in Selection.objects)
            {
                if (selected == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(selected);
                if (selected is GameObject gameObject && string.IsNullOrWhiteSpace(assetPath))
                {
                    objects.Add(CreateObjectInfo(gameObject));
                }
                else if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    assets.Add(CreateAssetInfo(selected, assetPath));
                }
            }
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
            return info;
        }

        private static BridgeAssetInfo CreateAssetInfo(UnityObject asset, string assetPath)
        {
            BridgeAssetInfo info = BridgeAssetInfo.Create();
            info.AssetPath = assetPath;
            info.Guid = AssetDatabase.AssetPathToGUID(assetPath);
            info.TypeName = asset.GetType().Name;
            info.Name = asset.name;
            info.Extension = Path.GetExtension(assetPath);
            info.FileSize = GetFileSize(assetPath);
            info.InstanceId = asset.GetInstanceID();
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

        private static long GetFileSize(string assetPath)
        {
            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('\\', '/')));
                return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
