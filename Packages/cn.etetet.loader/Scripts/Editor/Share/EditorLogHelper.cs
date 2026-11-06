using UnityEditor;

namespace ET
{
    /// <summary>
    /// Unity 编辑器非运行状态（EditMode）下的全局日志与参数单例维护工具。
    /// 确保在编辑器脚本（如自定义菜单、工具窗口）中调用 <see cref="Log.Info(string)"/> 或访问 <see cref="Options.Instance"/> 时不会因 <see cref="World"/> 未初始化而抛空引用异常；
    /// 并在即将进入 PlayMode 时清理编辑态单例，避免污染运行态的 <see cref="World"/>。
    /// </summary>
    [InitializeOnLoad]
    public class EditorLogHelper
    {
        /// <summary>
        /// 静态构造函数：监听 PlayMode 状态切换与脚本编译完成事件。
        /// </summary>
        static EditorLogHelper()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += CheckCompolingFinish;
        }

        /// <summary>
        /// 当脚本编译完成后，初始化编辑态下的 <see cref="Logger"/> 和 <see cref="Options"/> 单例。
        /// </summary>
        private static void CheckCompolingFinish()
        {
            if (!EditorApplication.isCompiling)
            {
                CreateLog();
                EditorApplication.update -= CheckCompolingFinish;
            }
        }

        /// <summary>
        /// 响应编辑器 PlayMode 切换：退出运行或回到编辑模式时重建编辑态日志单例，即将退出编辑模式进入运行态时销毁 <see cref="World"/>。
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    CreateLog();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    DestroyLog();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 创建编辑态默认的 <see cref="Logger"/>（使用 <c>UnityLogger("Editor")</c>）与 <see cref="Options"/> 单例。
        /// </summary>
        private static void CreateLog()
        {
            if (Logger.Instance == null)
            {
                World.Instance.AddSingleton<Logger>().Log = new UnityLogger("Editor");
            }

            if (Options.Instance == null)
            {
                World.Instance.AddSingleton(new Options());
            }
        }

        /// <summary>
        /// 销毁全局 <see cref="World"/> 单例容器。
        /// </summary>
        private static void DestroyLog()
        {
            World.Instance.Dispose();
        }
    }
}