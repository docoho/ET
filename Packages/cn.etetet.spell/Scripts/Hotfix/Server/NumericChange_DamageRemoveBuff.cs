using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端监听生命值（<see cref="NumericType.HP"/>）变化，当受到伤害（HP 减少）时自动移除带有 <see cref="BuffFlags.DamageRemove"/> 标记的 Buff。
    /// </summary>
    [NumericWatcher(SceneType.Map, NumericType.HP)]
    public class NumericChange_DamageRemoveBuff: INumericWatcher
    {
        /// <summary>
        /// 若新 HP 小于旧 HP，则移除单位身上所有带 <see cref="BuffFlags.DamageRemove"/> 标记的 Buff。
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            if (args.New >= args.Old)
            {
                return;
            }

            BuffHelper.RemoveBuffFlag(unit, BuffFlags.DamageRemove);
        }
    }
}