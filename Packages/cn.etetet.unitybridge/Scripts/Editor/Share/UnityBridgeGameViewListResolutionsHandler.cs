using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 枚举当前平台 GameView 所有可用分辨率列表及当前选中索引的命令处理器（GameViewListResolutions）。
    /// </summary>
    internal sealed class UnityBridgeGameViewListResolutionsHandler : AUnityBridgeHandler<GameViewListResolutionsRequest, GameViewListResolutionsResponse>
    {
        protected override async ETTask<IResponse> Run(GameViewListResolutionsRequest command)
        {
            await ETTask.CompletedTask;

            GameViewListResolutionsResponse response = GameViewListResolutionsResponse.Create();
            try
            {
                if (!TryGetResolutionList(response, out string error))
                {
                    response.Error = UnityBridgeErrorCode.HandlerFail;
                    response.Message = error;
                }

                return response;
            }
            catch (Exception e)
            {
                response.Error = UnityBridgeErrorCode.HandlerFail;
                response.Message = e.ToString();
                return response;
            }
        }

        private static bool TryGetResolutionList(GameViewListResolutionsResponse response, out string error)
        {
            error = null;

            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            object gameView = GetMainGameView(gameViewType);
            object group = GetCurrentSizeGroup(out error);
            if (group == null)
            {
                error ??= "could not access Game view size group";
                return false;
            }

            int selectedIndex = gameView == null ? -1 : GetSelectedSizeIndex(gameViewType, gameView);
            int totalCount = GetTotalCount(group);
            for (int i = 0; i < totalCount; ++i)
            {
                object size = GetGameViewSize(group, i);
                if (size == null)
                {
                    continue;
                }

                response.Resolutions.Add(CreateResolution(size, i == selectedIndex));
            }

            response.Count = response.Resolutions.Count;
            response.CurrentIndex = selectedIndex;
            if (response.Count <= 0)
            {
                error = "Game view resolution list is empty";
                return false;
            }

            return true;
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
