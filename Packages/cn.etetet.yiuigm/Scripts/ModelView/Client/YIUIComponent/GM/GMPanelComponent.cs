using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// GM悬浮入口面板组件，维护悬浮按钮的快捷键、本地持久化的XY坐标及拖拽偏移。
    /// 文档: https://lib9kmxvq7k.feishu.cn/wiki/NYADwMydliVmQ7kWXOuc0yxGn7p
    /// </summary>
    public partial class GMPanelComponent : Entity, IUpdate
    {
        public KeyCode    _OpenGMViewKey     = KeyCode.None;
        public FloatPrefs _GMBtn_Pos_X = new("GMPanelComponent_GMBtn_Pos_X");
        public FloatPrefs _GMBtn_Pos_Y = new("GMPanelComponent_GMBtn_Pos_Y");
        public Vector2    _Offset;
        public Vector2    _LimitSize;
    }
}