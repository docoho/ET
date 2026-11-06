using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 获取当前 GameView 窗口选中分辨率与尺寸类型的命令处理器（GameViewGetResolution）。
    /// </summary>
    internal sealed class UnityBridgeGameViewGetResolutionHandler : AUnityBridgeHandler<GameViewGetResolutionRequest, GameViewGetResolutionResponse>
    {
        protected override async ETTask<IResponse> Run(GameViewGetResolutionRequest command)
        {
            await ETTask.CompletedTask;

            GameViewGetResolutionResponse response = GameViewGetResolutionResponse.Create();
            try
            {
                if (!TryGetCurrentResolution(out BridgeGameViewResolution resolution, out int selectedIndex, out string sizeType, out string error))
                {
                    response.Error = UnityBridgeErrorCode.HandlerFail;
                    response.Message = error;
                    return response;
                }

                response.Resolution = resolution;
                response.SelectedIndex = selectedIndex;
                response.SizeType = sizeType;
                return response;
            }
            catch (Exception e)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = e.ToString();
                return response;
            }
        }

        private static bool TryGetCurrentResolution(out BridgeGameViewResolution resolution, out int selectedIndex, out string sizeType, out string error)
        {
            resolution = null;
            selectedIndex = -1;
            sizeType = string.Empty;
            error = null;

            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            object gameView = GetMainGameView(gameViewType);
            object group = GetCurrentSizeGroup(out error);
            if (group == null)
            {
                error ??= "could not access Game view size group";
                return false;
            }

            int totalCount = GetTotalCount(group);
            selectedIndex = gameView == null ? -1 : GetSelectedSizeIndex(gameViewType, gameView);
            if (selectedIndex >= 0 && selectedIndex < totalCount)
            {
                object size = GetGameViewSize(group, selectedIndex);
                resolution = CreateResolution(size, true);
                sizeType = GetSizeType(size);
                return true;
            }

            if (TryGetTargetSize(gameViewType, gameView, out int width, out int height))
            {
                resolution = BridgeGameViewResolution.Create();
                resolution.Width = width;
                resolution.Height = height;
                resolution.Label = "Unknown";
                resolution.IsCurrent = true;
                sizeType = "Unknown";
                return true;
            }

            error = "no Game view window or current resolution found";
            return false;
        }

        private static object GetMainGameView(Type gameViewType)
        {
            if (gameViewType == null)
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            MethodInfo getMainGameView = gameViewType.GetMethod("GetMainGameView", flags) ??
                    gameViewType.GetMethod("GetMainPlayModeView", flags);
            if (getMainGameView != null)
            {
                object gameView = getMainGameView.Invoke(null, null);
                if (gameView != null)
                {
                    return gameView;
                }
            }

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(gameViewType);
            return windows != null && windows.Length > 0 ? windows[0] : null;
        }

        private static object GetCurrentSizeGroup(out string error)
        {
            error = null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            Type sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            if (sizesType == null)
            {
                error = "GameViewSizes type not found";
                return null;
            }

            object sizesInstance = GetSizesInstance(sizesType, flags);
            if (sizesInstance == null)
            {
                error = "GameViewSizes instance not found";
                return null;
            }

            PropertyInfo currentGroupProperty = sizesType.GetProperty("currentGroup", flags);
            if (currentGroupProperty != null)
            {
                return currentGroupProperty.GetValue(sizesInstance);
            }

            PropertyInfo currentGroupTypeProperty = sizesType.GetProperty("currentGroupType", flags);
            MethodInfo getGroupMethod = sizesType.GetMethod("GetGroup", flags);
            if (currentGroupTypeProperty == null || getGroupMethod == null)
            {
                error = "GameView size group members not found";
                return null;
            }

            object groupTypeValue = currentGroupTypeProperty.GetGetMethod(true)?.IsStatic == true
                    ? currentGroupTypeProperty.GetValue(null)
                    : currentGroupTypeProperty.GetValue(sizesInstance);
            return getGroupMethod.Invoke(sizesInstance, new object[] { Convert.ToInt32(groupTypeValue) });
        }

        private static object GetSizesInstance(Type sizesType, BindingFlags flags)
        {
            Type current = sizesType;
            while (current != null)
            {
                PropertyInfo instanceProperty = current.GetProperty("instance", flags);
                if (instanceProperty != null)
                {
                    return instanceProperty.GetValue(null);
                }

                current = current.BaseType;
            }

            return null;
        }

        private static int GetSelectedSizeIndex(Type gameViewType, object gameView)
        {
            PropertyInfo selectedSizeIndex = gameViewType?.GetProperty("selectedSizeIndex",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return selectedSizeIndex == null || gameView == null ? -1 : (int)selectedSizeIndex.GetValue(gameView);
        }

        private static int GetTotalCount(object group)
        {
            MethodInfo method = group.GetType().GetMethod("GetTotalCount", BindingFlags.Public | BindingFlags.Instance);
            return method == null ? 0 : (int)method.Invoke(group, null);
        }

        private static object GetGameViewSize(object group, int index)
        {
            MethodInfo method = group.GetType().GetMethod("GetGameViewSize", BindingFlags.Public | BindingFlags.Instance);
            return method?.Invoke(group, new object[] { index });
        }

        private static BridgeGameViewResolution CreateResolution(object size, bool isCurrent)
        {
            BridgeGameViewResolution resolution = BridgeGameViewResolution.Create();
            resolution.Width = GetIntProperty(size, "width");
            resolution.Height = GetIntProperty(size, "height");
            resolution.Label = GetStringProperty(size, "baseText");
            resolution.IsCurrent = isCurrent;
            return resolution;
        }

        private static string GetSizeType(object size)
        {
            object value = size?.GetType().GetProperty("sizeType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(size);
            return value?.ToString() ?? string.Empty;
        }

        private static bool TryGetTargetSize(Type gameViewType, object gameView, out int width, out int height)
        {
            width = 0;
            height = 0;
            PropertyInfo targetSizeProperty = gameViewType?.GetProperty("targetSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetSizeProperty == null || gameView == null)
            {
                return false;
            }

            Vector2 targetSize = (Vector2)targetSizeProperty.GetValue(gameView);
            width = (int)targetSize.x;
            height = (int)targetSize.y;
            return width > 0 && height > 0;
        }

        private static int GetIntProperty(object instance, string propertyName)
        {
            object value = instance?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
            return value is int intValue ? intValue : 0;
        }

        private static string GetStringProperty(object instance, string propertyName)
        {
            object value = instance?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
            return value?.ToString() ?? string.Empty;
        }
    }
}
