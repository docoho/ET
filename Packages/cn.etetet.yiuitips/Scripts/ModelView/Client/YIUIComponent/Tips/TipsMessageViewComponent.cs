using System;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 通用确认/取消消息弹窗视图实体。
    /// </summary>
    public partial class TipsMessageViewComponent : Entity, IYIUIOpen<ParamVo>, IYIUIOpenTween, IYIUICloseTween
    {
        public MessageTipsExtraData ExtraData;
    }

    //额外参数
    /// <summary>
    /// 消息弹窗附加配置参数（自定义确认/取消按钮文字、控制取消与关闭按钮显隐）。
    /// </summary>
    [EnableClass]
    public class MessageTipsExtraData
    {
        public string ConfirmName; //确定按钮换名字
        public string CancelName; //取消按钮换名字
        public bool ShowCancelButton; //显示取消按钮
        public bool ShowCloseButton; //显示关闭按钮
    }
}