using Coffee.UIEffects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUIFramework
{
    /// <summary>
    /// YIUI布尔数据驱动的UI置灰绑定组件。
    /// 结合UIEffect组件，根据绑定的布尔值结果动态切换灰度滤镜强度（ToneFilter.Grayscale）。
    /// </summary>
    [LabelText("置灰")]
    [RequireComponent(typeof(UIEffect))]
    [AddComponentMenu("YIUIBind/Data/置灰 【Gray】 UIDataBindGray")]
    public sealed class UIDataBindGray : UIDataBindBool
    {
        [SerializeField]
        [Range(0, 1)]
        [LabelText("启用时灰度值")]
        private float m_EnabledGray = 1;

        [SerializeField]
        [Range(0, 1)]
        [LabelText("禁用时灰度值")]
        private float m_DisabledGray = 0;

        [SerializeField]
        [ReadOnly]
        [Required("必须有此组件")]
        [LabelText("UI灰度")]
        private UIEffect m_Grayscale;

        protected override void OnRefreshData()
        {
            base.OnRefreshData();
            m_Grayscale            ??= GetComponent<UIEffect>();
            m_Grayscale.toneFilter =   ToneFilter.Grayscale;
        }

        protected override void OnValueChanged()
        {
            if (m_Grayscale == null) return;
            m_Grayscale.toneIntensity = GetResult() ? m_EnabledGray : m_DisabledGray;
        }
    }
}