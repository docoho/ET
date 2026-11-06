using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 重置指定 Transform 的局部位置、旋转或缩放（支持 Undo）的命令处理器（TransformReset）。
    /// </summary>
    internal sealed class UnityBridgeTransformResetHandler : AUnityBridgeHandler<TransformResetRequest, TransformResetResponse>
    {
        protected override async ETTask<IResponse> Run(TransformResetRequest command)
        {
            await ETTask.CompletedTask;

            TransformResetResponse response = TransformResetResponse.Create();
            Transform transform = FindTransform(command.Path, command.InstanceId);
            if (transform == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "Transform not found";
                return response;
            }

            bool resetAll = !command.ResetPosition && !command.ResetRotation && !command.ResetScale;
            Undo.RecordObject(transform, $"Reset Transform {transform.name}");
            if (resetAll || command.ResetPosition)
            {
                transform.localPosition = Vector3.zero;
            }

            if (resetAll || command.ResetRotation)
            {
                transform.localRotation = Quaternion.identity;
            }

            if (resetAll || command.ResetScale)
            {
                transform.localScale = Vector3.one;
            }

            response.Name = transform.name;
            response.Path = GetPath(transform.gameObject);
            response.Transform = CreateTransformInfo(transform);
            return response;
        }

        private static Transform FindTransform(string path, int instanceId)
        {
            if (instanceId != 0)
            {
#if UNITY_6000_3_OR_NEWER
                return (EditorUtility.EntityIdToObject(instanceId) as GameObject)?.transform;
#else
                return (EditorUtility.InstanceIDToObject(instanceId) as GameObject)?.transform;
#endif
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return Selection.activeTransform;
            }

            GameObject gameObject = GameObject.Find(path);
            if (gameObject != null)
            {
                return gameObject.transform;
            }

            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate == null || EditorUtility.IsPersistent(candidate) || !candidate.scene.IsValid())
                {
                    continue;
                }

                if (GetPath(candidate) == path)
                {
                    return candidate.transform;
                }
            }

            return null;
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
