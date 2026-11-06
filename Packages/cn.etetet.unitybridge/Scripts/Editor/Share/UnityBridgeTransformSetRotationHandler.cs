using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 设置指定 Transform 的世界欧拉角或局部欧拉角旋转（支持 Undo）的命令处理器（TransformSetRotation）。
    /// </summary>
    internal sealed class UnityBridgeTransformSetRotationHandler : AUnityBridgeHandler<TransformSetRotationRequest, TransformSetRotationResponse>
    {
        protected override async ETTask<IResponse> Run(TransformSetRotationRequest command)
        {
            await ETTask.CompletedTask;

            TransformSetRotationResponse response = TransformSetRotationResponse.Create();
            Transform transform = FindTransform(command.Path, command.InstanceId);
            if (transform == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "Transform not found";
                return response;
            }

            if (command.EulerAngles == null)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "EulerAngles is required";
                return response;
            }

            Undo.RecordObject(transform, $"Set Rotation {transform.name}");
            Vector3 eulerAngles = new(command.EulerAngles.X, command.EulerAngles.Y, command.EulerAngles.Z);
            if (command.Local)
            {
                transform.localEulerAngles = eulerAngles;
            }
            else
            {
                transform.eulerAngles = eulerAngles;
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
