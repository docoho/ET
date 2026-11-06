#if TextMeshPro
using TMPro;
using UnityEngine;
using Sirenix.OdinInspector;

namespace YIUIFramework
{
    /// <summary>
    /// 携带TextMeshProUGUI数字显示的红点绑定组件。
    /// </summary>
    [AddComponentMenu("YIUIFramework/红点/红点TMP绑定 【RedDotTextBind】")]
    public class RedDotTmpBind : RedDotBind
    {
        [PropertyOrder(int.MinValue)]
        [SerializeField]
        [LabelText("文本")]
        [EnableIf("@UIOperationHelper.CommonShowIf()")]
        private TextMeshProUGUI m_Text;

        protected override void ChangeText()
        {
            if (m_Text != null)
            {
                m_Text.text = Count.ToString();
            }
        }
    }
}
#endif