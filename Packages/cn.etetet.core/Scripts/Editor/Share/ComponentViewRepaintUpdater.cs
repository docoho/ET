#if ENABLE_VIEW && UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// Unity 编辑器下的 <see cref="ComponentView"/> 检视面板定时重绘更新器。
    /// 当开发者在 Hierarchy 中选中挂载了 <see cref="ComponentView"/> 的实体 GameObject 时，
    /// 每隔 0.3 秒触发一次编辑器视图重绘，从而让 Inspector 面板能够实时刷新显示 ET 实体属性的最新运行状态。
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentViewRepaintUpdater
    {
        /// <summary>
        /// 重绘时间间隔（0.3 秒），避免每帧重绘造成编辑器卡顿。
        /// </summary>
        private const double RepaintIntervalSeconds = 0.3d;
        private static double nextRepaintTime;

        /// <summary>
        /// 静态构造函数：在 Unity 编辑器加载或脚本编译完成后自动订阅 <see cref="EditorApplication.update"/> 事件。
        /// </summary>
        static ComponentViewRepaintUpdater()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// 编辑器每帧回调：检查当前选中的对象是否包含激活的 <see cref="ComponentView"/>，若满足重绘时间间隔则刷新所有视图。
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextRepaintTime)
            {
                return;
            }

            GameObject activeObject = Selection.activeGameObject;
            if (activeObject == null)
            {
                return;
            }

            ComponentView view = activeObject.GetComponent<ComponentView>();
            if (view == null || !view.isActiveAndEnabled)
            {
                return;
            }

            nextRepaintTime = EditorApplication.timeSinceStartup + RepaintIntervalSeconds;
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
#endif
