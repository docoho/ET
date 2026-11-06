using System.Collections.Generic;
using UnityEngine;

namespace YIUIFramework
{
    /// <summary>
    /// UI 3D展示相机控制组件。
    /// 负责在启用/禁用或切换展示模型时，自动将目标模型及其所有子Renderer切换到专属3D展示Layer，避免与主场景穿插渲染。
    /// </summary>
    public sealed class UI3DDisplayCamera : MonoBehaviour
    {
        private GameObject m_ShowObject;

        /// <summary>
        /// 获取或设置当前正在展示的3D模型根对象（切换时会自动重置旧模型的Layer并设置新模型的Layer）。
        /// </summary>
        internal GameObject ShowObject
        {
            get => m_ShowObject;
            set
            {
                if (m_ShowObject != null &&
                    m_ShowObject != value)
                {
                    ResetRenderer(m_ShowObject.transform);
                }

                m_ShowObject = value;
                if (value != null)
                    SetupRenderer(value.transform);
            }
        }

        private int m_ShowLayer = 0;

        internal int ShowLayer
        {
            get => m_ShowLayer;
            set
            {
                if (m_ShowLayer != value)
                {
                    m_ShowLayer = value;
                    if (ShowObject != null)
                        SetupRenderer(ShowObject.transform);
                }
            }
        }

        private static void ResetRenderer(Transform transform)
        {
            var renderers = ListPool<Renderer>.Get();
            transform.GetComponentsInChildren(true, renderers);
            foreach (var renderer in renderers)
            {
                renderer.gameObject.layer = 0;
            }
            ListPool<Renderer>.Put(renderers);
        }

        public void SetupRenderer(Transform transform)
        {
            if (ShowObject == null) return;

            var renderers = ListPool<Renderer>.Get();
            transform.GetComponentsInChildren(true, renderers);
            foreach (var renderer in renderers)
            {
                renderer.gameObject.layer = ShowLayer;
            }
            ListPool<Renderer>.Put(renderers);
        }

        private void OnEnable()
        {
            if (ShowObject != null)
            {
                SetupRenderer(ShowObject.transform);
            }
        }

        private void OnDisable()
        {
            if (ShowObject != null)
            {
                ResetRenderer(ShowObject.transform);
            }
        }
    }
}
