using System;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge Inspector 属性与组件操作处理器抽象基类，封装场景对象、Prefab 资产与普通资源的定位解析、SerializedProperty 读写以及修改保存逻辑。
    /// </summary>
    internal abstract class AUnityBridgeInspectorHandler<TRequest, TResponse> : AUnityBridgeHandler<TRequest, TResponse>
            where TRequest : class, IRequest
            where TResponse : class, IResponse
    {
        protected const string AssetsPathPrefix = "Assets/";
        protected const string PackagesPathPrefix = "Packages/";

        protected sealed class TargetContext : IDisposable
        {
            public string AssetPath;
            public string ObjectPath;
            public GameObject PrefabRoot;
            public GameObject GameObject;
            public UnityEngine.Object SerializedTarget;
            public bool IsPrefabAsset;
            public bool IsAssetObject;
            public bool IsSceneObject;

            public void Dispose()
            {
                if (this.IsPrefabAsset && this.PrefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(this.PrefabRoot);
                    this.PrefabRoot = null;
                }
            }
        }

        protected static bool TryResolveTargetContext(
                string path,
                int instanceId,
                string assetPath,
                string objectPath,
                bool forWrite,
                out TargetContext context,
                out string error)
        {
            context = null;
            error = null;

            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                if (!IsUnityAssetPath(assetPath))
                {
                    error = "assetPath must start with Assets/ or Packages/";
                    return false;
                }

                if (forWrite && assetPath.StartsWith(PackagesPathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    error = "editing package assets is not supported";
                    return false;
                }

                if (assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    return TryResolvePrefabAssetContext(assetPath, objectPath, out context, out error);
                }

                if (!string.IsNullOrWhiteSpace(objectPath))
                {
                    error = "objectPath is only supported for prefab assets";
                    return false;
                }

                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset == null)
                {
                    error = $"asset not found: {assetPath}";
                    return false;
                }

                context = new TargetContext
                {
                    AssetPath = assetPath,
                    SerializedTarget = asset,
                    GameObject = asset as GameObject,
                    IsAssetObject = true
                };
                return true;
            }

            GameObject gameObject = GetSceneGameObject(path, instanceId);
            if (gameObject == null)
            {
                error = "GameObject not found";
                return false;
            }

            context = new TargetContext
            {
                GameObject = gameObject,
                SerializedTarget = gameObject,
                IsSceneObject = true
            };
            return true;
        }

        protected static bool TryResolveSerializedTarget(
                TargetContext context,
                string componentName,
                int componentIndex,
                int componentInstanceId,
                out UnityEngine.Object serializedTarget,
                out Component component,
                out string error)
        {
            serializedTarget = null;
            component = null;
            error = null;

            if (context == null)
            {
                error = "target context is null";
                return false;
            }

            if (context.SerializedTarget is Component existingComponent)
            {
                component = existingComponent;
                serializedTarget = existingComponent;
                return true;
            }

            if (context.GameObject != null)
            {
                component = ResolveComponent(context, componentName, componentIndex, componentInstanceId);
                if (component == null)
                {
                    error = "Component not found. Provide ComponentName, ComponentIndex, or ComponentInstanceId";
                    return false;
                }

                serializedTarget = component;
                return true;
            }

            if (context.SerializedTarget != null)
            {
                if (!string.IsNullOrWhiteSpace(componentName) || componentIndex >= 0 || componentInstanceId != 0)
                {
                    error = "component selectors can only be used with GameObject targets";
                    return false;
                }

                serializedTarget = context.SerializedTarget;
                return true;
            }

            error = "serialized target not found";
            return false;
        }

        protected static Component ResolveComponent(TargetContext context, string componentName, int componentIndex, int componentInstanceId)
        {
            GameObject gameObject = context?.GameObject;
            if (gameObject == null)
            {
                return null;
            }

            if (componentInstanceId != 0 && context.IsSceneObject)
            {
                if (GetObjectByInstanceId(componentInstanceId) is Component componentById && componentById.gameObject == gameObject)
                {
                    return componentById;
                }
            }

            Component[] components = gameObject.GetComponents<Component>();
            if (componentIndex >= 0)
            {
                return componentIndex < components.Length ? components[componentIndex] : null;
            }

            if (string.IsNullOrWhiteSpace(componentName))
            {
                return null;
            }

            foreach (Component component in components)
            {
                if (component != null && (component.GetType().Name == componentName || component.GetType().FullName == componentName))
                {
                    return component;
                }
            }

            return null;
        }

        protected static BridgeComponentInfo CreateComponentInfo(Component component, int index)
        {
            BridgeComponentInfo info = BridgeComponentInfo.Create();
            info.TypeName = component.GetType().Name;
            info.FullTypeName = component.GetType().FullName;
            info.ComponentIndex = index;
            info.InstanceId = component.GetInstanceID();
            info.Enabled = component switch
            {
                Collider2D collider2D => collider2D.enabled,
                Collider collider => collider.enabled,
                Behaviour behaviour => behaviour.enabled,
                Renderer renderer => renderer.enabled,
                _ => true
            };
            return info;
        }

        protected static BridgePropertyInfo CreatePropertyInfo(SerializedProperty property)
        {
            BridgePropertyInfo info = BridgePropertyInfo.Create();
            info.Name = property.name;
            info.PropertyPath = property.propertyPath;
            info.DisplayName = property.displayName;
            info.Type = property.propertyType.ToString();
            info.IsArray = property.isArray;
            info.IsEditable = property.editable;
            info.IsExpanded = property.isExpanded;
            info.HasChildren = property.hasChildren;
            info.Depth = property.depth;
            FillPropertyValue(info, property);
            return info;
        }

        protected static bool TrySetPropertyValue(SerializedProperty property, BridgePropertyInfo value, out string error)
        {
            error = null;
            if (value == null)
            {
                error = "value is null";
                return false;
            }

            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                    case SerializedPropertyType.ArraySize:
                    case SerializedPropertyType.Character:
                        property.intValue = value.IntValue;
                        return true;
                    case SerializedPropertyType.Boolean:
                        property.boolValue = value.BoolValue;
                        return true;
                    case SerializedPropertyType.Float:
                        property.floatValue = value.FloatValue;
                        return true;
                    case SerializedPropertyType.String:
                        property.stringValue = value.StringValue ?? string.Empty;
                        return true;
                    case SerializedPropertyType.Enum:
                        return TrySetEnumValue(property, value, out error);
                    case SerializedPropertyType.ObjectReference:
                        return TrySetObjectReference(property, value.ObjectReferencePath, out error);
                    case SerializedPropertyType.Vector2:
                        if (value.Vector2Value == null)
                        {
                            error = "Vector2Value is required";
                            return false;
                        }

                        property.vector2Value = new Vector2(value.Vector2Value.X, value.Vector2Value.Y);
                        return true;
                    case SerializedPropertyType.Vector3:
                        if (value.Vector3Value == null)
                        {
                            error = "Vector3Value is required";
                            return false;
                        }

                        property.vector3Value = new Vector3(value.Vector3Value.X, value.Vector3Value.Y, value.Vector3Value.Z);
                        return true;
                    default:
                        error = $"unsupported property type: {property.propertyType}";
                        return false;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        protected static bool TrySaveModifiedTarget(TargetContext context, UnityEngine.Object serializedTarget, out string error)
        {
            error = null;
            if (context == null)
            {
                return true;
            }

            if (context.IsPrefabAsset && context.PrefabRoot != null)
            {
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(context.PrefabRoot, context.AssetPath, out bool success);
                if (!success || savedPrefab == null)
                {
                    error = $"failed to save prefab asset: {context.AssetPath}";
                    return false;
                }

                AssetDatabase.ImportAsset(context.AssetPath);
                return true;
            }

            if (context.IsAssetObject && serializedTarget != null)
            {
                EditorUtility.SetDirty(serializedTarget);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(context.AssetPath);
            }

            return true;
        }

        protected static Type ResolveComponentType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            string[] possibleNames =
            {
                typeName,
                $"UnityEngine.{typeName}",
                $"UnityEngine.UI.{typeName}",
                $"TMPro.{typeName}"
            };

            foreach (string name in possibleNames)
            {
                Type type = Type.GetType(name);
                if (type != null)
                {
                    return type;
                }

                foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(name);
                    if (type != null)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static bool TryResolvePrefabAssetContext(string assetPath, string objectPath, out TargetContext context, out string error)
        {
            context = null;
            error = null;

            GameObject root;
            try
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
            }
            catch (Exception e)
            {
                error = $"failed to load prefab contents: {e.Message}";
                return false;
            }

            if (root == null)
            {
                error = $"prefab not found: {assetPath}";
                return false;
            }

            GameObject target = ResolvePrefabObject(root, objectPath);
            if (target == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                error = $"object not found in prefab: {objectPath}";
                return false;
            }

            context = new TargetContext
            {
                AssetPath = assetPath,
                ObjectPath = string.IsNullOrWhiteSpace(objectPath) ? target.name : objectPath,
                PrefabRoot = root,
                GameObject = target,
                SerializedTarget = target,
                IsPrefabAsset = true,
                IsAssetObject = true
            };
            return true;
        }

        private static GameObject ResolvePrefabObject(GameObject root, string objectPath)
        {
            if (root == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(objectPath) || objectPath == "." || objectPath == "/" || objectPath == root.name)
            {
                return root;
            }

            string normalized = objectPath.Replace('\\', '/').Trim('/');
            if (normalized.StartsWith(root.name + "/", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(root.name.Length + 1);
            }

            Transform child = root.transform.Find(normalized);
            return child != null ? child.gameObject : FindChildByName(root.transform, normalized);
        }

        private static GameObject FindChildByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }

                GameObject found = FindChildByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject GetSceneGameObject(string path, int instanceId)
        {
            if (instanceId != 0)
            {
                return GetObjectByInstanceId(instanceId) as GameObject;
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

        private static UnityEngine.Object GetObjectByInstanceId(int instanceId)
        {
#if UNITY_6000_3_OR_NEWER
            return EditorUtility.EntityIdToObject(instanceId);
#else
            return EditorUtility.InstanceIDToObject(instanceId);
#endif
        }

        private static bool IsUnityAssetPath(string assetPath)
        {
            return assetPath.StartsWith(AssetsPathPrefix, StringComparison.OrdinalIgnoreCase)
                   || assetPath.StartsWith(PackagesPathPrefix, StringComparison.OrdinalIgnoreCase);
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

        private static void FillPropertyValue(BridgePropertyInfo info, SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                    info.IntValue = property.intValue;
                    info.StringValue = property.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    info.BoolValue = property.boolValue;
                    info.StringValue = property.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    info.FloatValue = property.floatValue;
                    info.StringValue = property.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.String:
                    info.StringValue = property.stringValue;
                    break;
                case SerializedPropertyType.Enum:
                    info.IntValue = property.enumValueIndex;
                    info.StringValue = property.enumValueIndex >= 0 && property.enumValueIndex < property.enumNames.Length
                            ? property.enumNames[property.enumValueIndex]
                            : string.Empty;
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (property.objectReferenceValue != null)
                    {
                        info.ObjectReferencePath = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                        info.ObjectReferenceType = property.objectReferenceValue.GetType().Name;
                        info.StringValue = property.objectReferenceValue.name;
                    }

                    break;
                case SerializedPropertyType.Vector2:
                    BridgeVector2 vector2 = BridgeVector2.Create();
                    vector2.X = property.vector2Value.x;
                    vector2.Y = property.vector2Value.y;
                    info.Vector2Value = vector2;
                    break;
                case SerializedPropertyType.Vector3:
                    BridgeVector3 vector3 = BridgeVector3.Create();
                    vector3.X = property.vector3Value.x;
                    vector3.Y = property.vector3Value.y;
                    vector3.Z = property.vector3Value.z;
                    info.Vector3Value = vector3;
                    break;
                default:
                    info.StringValue = property.propertyType.ToString();
                    break;
            }
        }

        private static bool TrySetEnumValue(SerializedProperty property, BridgePropertyInfo value, out string error)
        {
            error = null;
            if (!string.IsNullOrWhiteSpace(value.StringValue))
            {
                for (int i = 0; i < property.enumNames.Length; ++i)
                {
                    if (property.enumNames[i] == value.StringValue)
                    {
                        property.enumValueIndex = i;
                        return true;
                    }
                }
            }

            if (value.IntValue >= 0 && value.IntValue < property.enumNames.Length)
            {
                property.enumValueIndex = value.IntValue;
                return true;
            }

            error = "invalid enum value";
            return false;
        }

        private static bool TrySetObjectReference(SerializedProperty property, string objectReferencePath, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(objectReferencePath) || string.Equals(objectReferencePath, "null", StringComparison.OrdinalIgnoreCase))
            {
                property.objectReferenceValue = null;
                return true;
            }

            UnityEngine.Object target = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(objectReferencePath);
            if (target == null)
            {
                string guidPath = AssetDatabase.GUIDToAssetPath(objectReferencePath);
                if (!string.IsNullOrWhiteSpace(guidPath))
                {
                    target = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(guidPath);
                }
            }

            if (target == null)
            {
                GameObject gameObject = GameObject.Find(objectReferencePath);
                target = gameObject;
            }

            if (target == null)
            {
                error = $"object reference target not found: {objectReferencePath}";
                return false;
            }

            property.objectReferenceValue = target;
            return true;
        }
    }
}
