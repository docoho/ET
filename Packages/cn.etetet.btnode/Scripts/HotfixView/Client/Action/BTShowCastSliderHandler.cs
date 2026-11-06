using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端显示施法读条/吟唱进度条节点处理器，通过动态事件 BTEvent_ShowCastSlider 通知 UI 层更新。
    /// </summary>
    public class BTShowCastSliderHandler : ABTHandler<BTShowCastSlider>
    {
        protected override int Run(BTShowCastSlider node, BTEnv env)
        {
            Buff buff   = env.GetEntity<Buff>(node.Buff);
            Unit target = buff.Parent.GetParent<Unit>();

            Log.Info($"读条 {target.Config().Name}, {node.ShowDisplayName}, {buff.GetConfig().Desc}");

            buff.DynamicEvent(new BTEvent_ShowCastSlider
            {
                Unit            = target,
                Buff            = buff,
                ShowDisplayName = node.ShowDisplayName,
                IconName        = node.IconName,
                IsIncrease      = node.IsIncrease
            }).Coroutine();

            return 0;
        }
    }
}
