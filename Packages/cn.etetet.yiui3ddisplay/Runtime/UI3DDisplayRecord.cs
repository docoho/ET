using UnityEngine;

namespace YIUIFramework
{
    /// <summary>
    /// 用于记录3D展示子节点的原始Layer与Renderer可见状态，并在移除或销毁时恢复原始设置。
    /// </summary>
    public sealed class UI3DDisplayRecord : MonoBehaviour
    {
        private int               m_Layer;
        private bool              m_Visible;
        private Renderer          m_AttachRenderer;
        private UI3DDisplayCamera m_ShowCamera;

        /// <summary>
        /// 初始化记录组件的渲染器及相机引用，保存原始Layer和启用状态。
        /// </summary>
        internal void Initialize(Renderer renderer, UI3DDisplayCamera camera)
        {
            m_AttachRenderer = renderer;
            m_ShowCamera     = camera;
            m_Layer          = renderer.gameObject.layer;
            m_Visible        = renderer.enabled;
        }

        private static bool IsParentOf(Transform obj, Transform parent)
        {
            if (obj == parent)
            {
                return true;
            }

            if (obj.parent == null)
            {
                return false;
            }

            return IsParentOf(obj.parent, parent);
        }

        private void OnTransformParentChanged()
        {
            if (m_ShowCamera == null ||
                !IsParentOf(transform, m_ShowCamera.transform))
            {
                this.SafeDestroySelf();
            }
        }

        private void OnDestroy()
        {
            if (m_AttachRenderer != null)
            {
                m_AttachRenderer.enabled = m_Visible;
            }

            gameObject.layer = m_Layer;
        }
    }
}
