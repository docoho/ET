using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace YIUIFramework
{
    /// <summary>
    /// 携带UGUI Text数字显示的红点绑定组件。
    /// </summary>
    [AddComponentMenu("YIUIFramework/红点/红点Text绑定 【RedDotTextBind】")]
    public class RedDotTextBind : RedDotBind
    {
        [PropertyOrder(int.MinValue)]
        [SerializeField]
        [LabelText("文本")]
        [EnableIf("@UIOperationHelper.CommonShowIf()")]
        private Text m_Text;

        protected override void ChangeText()
        {
            if (m_Text != null)
            {
                m_Text.text = Count.ToString();
            }
        }
    }
}