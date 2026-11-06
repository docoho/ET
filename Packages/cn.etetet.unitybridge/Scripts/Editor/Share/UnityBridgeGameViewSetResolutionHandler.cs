using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 设置 GameView 窗口分辨率（若不存在指定宽高则自动新增自定义分辨率项）的命令处理器（GameViewSetResolution）。
    /// </summary>
    internal sealed class UnityBridgeGameViewSetResolutionHandler : AUnityBridgeHandler<GameViewSetResolutionRequest, GameViewSetResolutionResponse>
    {
        protected override async ETTask<IResponse> Run(GameViewSetResolutionRequest command)
        {
            await ETTask.CompletedTask;

            GameViewSetResolutionResponse response = GameViewSetResolutionResponse.Create();
            response.SelectedIndex = -1;
            if (command.Width <= 0 || command.Height <= 0)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = "GameView resolution width and height must be positive";
                return response;
            }

            if (command.Width > 8192 || command.Height > 8192)
            {
                response.Error = UnityBridgeErrorCode.InvalidCommandLine;
                response.Message = $"GameView resolution {command.Width}x{command.Height} exceeds maximum 8192x8192";
                return response;
            }

            try
            {
                if (!TrySetResolution(command.Width, command.Height, command.Label, response, out string error))
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

        private static bool TrySetResolution(int width, int height, string label, GameViewSetResolutionResponse response, out string error)
        {
            error = null;

            Assembly editorAssembly = typeof(EditorWindow).Assembly;
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            object gameView = GetMainGameView(gameViewType);
            if (gameView == null)
            {
                error = "no Game view window found";
                return false;
            }

            object group = GetCurrentSizeGroup(editorAssembly, out Type sizesType, out object sizesInstance, out error);
            if (group == null)
            {
                error ??= "could not access Game view size group";
                return false;
            }

            int selectedIndex = FindResolution(group, width, height);
            bool wasAdded = false;
            if (selectedIndex < 0)
            {
                string normalizedLabel = string.IsNullOrWhiteSpace(label) ? $"UnityBridge {width}x{height}" : label.Trim();
                selectedIndex = AddCustomResolution(editorAssembly, group, sizesType, sizesInstance, width, height, normalizedLabel);
                wasAdded = true;
            }

            object size = GetGameViewSize(group, selectedIndex);
            SetSelectedSizeIndex(gameViewType, gameView, selectedIndex);
            if (gameView is EditorWindow editorWindow)
            {
                editorWindow.Repaint();
            }

            response.Resolution = CreateResolution(size, true);
            response.SelectedIndex = selectedIndex;
            response.WasAdded = wasAdded;
            response.SizeType = GetSizeType(size);
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

        private static object GetCurrentSizeGroup(Assembly editorAssembly, out Type sizesType, out object sizesInstance, out string error)
        {
            error = null;
            sizesInstance = null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            if (sizesType == null)
            {
                error = "GameViewSizes type not found";
                return null;
            }

            sizesInstance = GetSizesInstance(sizesType, flags);
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

        private static int FindResolution(object group, int width, int height)
        {
            int totalCount = GetTotalCount(group);
            for (int i = 0; i < totalCount; ++i)
            {
                object size = GetGameViewSize(group, i);
                if (GetIntProperty(size, "width") == width && GetIntProperty(size, "height") == height)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int AddCustomResolution(Assembly editorAssembly, object group, Type sizesType, object sizesInstance, int width, int height, string label)
        {
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            if (sizeType == null || sizeTypeEnum == null)
            {
                throw new InvalidOperationException("GameViewSize internals not found");
            }

            object fixedResolutionValue = Enum.Parse(sizeTypeEnum, "FixedResolution");
            object newSize;
            ConstructorInfo constructor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            if (constructor != null)
            {
                newSize = constructor.Invoke(new[] { fixedResolutionValue, width, height, label });
            }
            else
            {
                newSize = Activator.CreateInstance(sizeType);
                SetProperty(newSize, "sizeType", fixedResolutionValue);
                SetProperty(newSize, "width", width);
                SetProperty(newSize, "height", height);
                SetProperty(newSize, "baseText", label);
            }

            MethodInfo addCustomSize = group.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            if (addCustomSize == null)
            {
                throw new InvalidOperationException("GameViewSizeGroup.AddCustomSize not found");
            }

            addCustomSize.Invoke(group, new[] { newSize });

            MethodInfo saveToHardDisk = sizesType?.GetMethod("SaveToHDD", BindingFlags.Public | BindingFlags.Instance);
            saveToHardDisk?.Invoke(sizesInstance, null);
            return GetTotalCount(group) - 1;
        }

        private static void SetSelectedSizeIndex(Type gameViewType, object gameView, int selectedIndex)
        {
            PropertyInfo selectedSizeIndex = gameViewType?.GetProperty("selectedSizeIndex",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            selectedSizeIndex?.SetValue(gameView, selectedIndex);
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

        private static void SetProperty(object instance, string propertyName, object value)
        {
            instance?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(instance, value);
        }
    }
}
