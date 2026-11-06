using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 截取 GameView 画面并编码保存为 PNG/JPG 图片文件的命令处理器（ScreenshotCapture）。
    /// </summary>
    internal sealed class UnityBridgeScreenshotCaptureHandler : AUnityBridgeHandler<ScreenshotCaptureRequest, ScreenshotCaptureResponse>
    {
        protected override async ETTask<IResponse> Run(ScreenshotCaptureRequest command)
        {
            await ETTask.CompletedTask;

            string target = NormalizeTarget(command.Target);
            string format = NormalizeFormat(command.Format);

            ScreenshotCaptureResponse response = ScreenshotCaptureResponse.Create();
            response.Target = target;
            if (!command.AllowEditMode && !EditorApplication.isPlaying)
            {
                response.Error = UnityBridgeErrorCode.NotInPlayMode;
                response.Message = "screenshot capture requires PlayMode unless AllowEditMode is true";
                response.Captured = false;
                return response;
            }

            Texture2D texture = null;
            try
            {
                texture = CaptureGameViewTexture();
                if (texture == null)
                {
                    response.Error = UnityBridgeErrorCode.HandlerFail;
                    response.Message = "failed to capture Game view texture";
                    response.Captured = false;
                    return response;
                }

                string directory = ResolveScreenshotDirectory();
                Directory.CreateDirectory(directory);

                string extension = format == "jpg" ? ".jpg" : ".png";
                string fileName = $"game_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
                string fullPath = Path.Combine(directory, fileName);
                byte[] bytes = EncodeTexture(texture, format, command.Quality);
                File.WriteAllBytes(fullPath, bytes);

                BridgeScreenshotInfo info = BridgeScreenshotInfo.Create();
                info.Path = fullPath;
                info.FileName = fileName;
                info.Width = texture.width;
                info.Height = texture.height;
                info.FileSize = bytes.Length;
                info.MediaType = format == "jpg" ? "image/jpeg" : "image/png";

                response.Captured = true;
                response.Screenshot = info;
                return response;
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        private static string NormalizeTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return "game";
            }

            string normalized = target.Trim().ToLowerInvariant();
            if (normalized == "game" || normalized == "gameview")
            {
                return "game";
            }

            throw new ArgumentException($"invalid screenshot target: {target}");
        }

        private static string NormalizeFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return "png";
            }

            string normalized = format.Trim().ToLowerInvariant();
            if (normalized == "png" || normalized == "jpg" || normalized == "jpeg")
            {
                return normalized == "jpeg" ? "jpg" : normalized;
            }

            throw new ArgumentException($"invalid screenshot format: {format}");
        }

        private static byte[] EncodeTexture(Texture2D texture, string format, int quality)
        {
            if (format == "jpg")
            {
                int normalizedQuality = quality > 0 ? Math.Clamp(quality, 1, 100) : 85;
                return texture.EncodeToJPG(normalizedQuality);
            }

            return texture.EncodeToPNG();
        }

        private static string ResolveScreenshotDirectory()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("unity project root cannot be resolved");
            }

            return Path.Combine(projectRoot, "Temp", "UnityBridge", "Screenshots");
        }

        private static Texture2D CaptureGameViewTexture()
        {
            RenderTexture renderTexture = GetGameViewRenderTexture();
            if (renderTexture != null)
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture copy = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height, 0, renderTexture.graphicsFormat);
                try
                {
                    if (SystemInfo.graphicsUVStartsAtTop)
                    {
                        Graphics.Blit(renderTexture, copy, new Vector2(1, -1), new Vector2(0, 1));
                    }
                    else
                    {
                        Graphics.Blit(renderTexture, copy);
                    }

                    Texture2D texture = new(copy.width, copy.height, TextureFormat.RGBA32, false);
                    RenderTexture.active = copy;
                    texture.ReadPixels(new Rect(0, 0, copy.width, copy.height), 0, 0);
                    texture.Apply();
                    return texture;
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(copy);
                }
            }

            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        private static RenderTexture GetGameViewRenderTexture()
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
            {
                return null;
            }

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows == null || windows.Length == 0)
            {
                return null;
            }

            FieldInfo renderTextureField = FindRenderTextureField(gameViewType);
            return renderTextureField?.GetValue(windows[0]) as RenderTexture;
        }

        private static FieldInfo FindRenderTextureField(Type gameViewType)
        {
            Type current = gameViewType;
            while (current != null && current != typeof(object))
            {
                FieldInfo field = current.GetField("m_RenderTexture", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return field;
                }

                current = current.BaseType;
            }

            return null;
        }
    }
}
