namespace ET.Client
{
    /// <summary>
    /// 客户端单位当前生命值（HP）变更监听器：派发 UI 动态事件刷新血条并触发战斗伤害/治疗飘字
    /// </summary>
    [NumericWatcher(SceneType.Current, NumericType.HP)]
    public class NumericWatcher_UnitInfo_Hp : INumericWatcher
    {
        /// <summary>
        /// 响应 HP 数值变更
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"HP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            unit.DynamicEvent(args).Coroutine();

            CombatTextHelper.ShowHpChange(unit.Scene(), unit, args.Old, args.New);
        }
    }

    /// <summary>
    /// 客户端单位最大生命值（MaxHP）变更监听器：派发 UI 动态事件刷新血条上限
    /// </summary>
    [NumericWatcher(SceneType.Current, NumericType.MaxHP)]
    public class NumericWatcher_UnitInfo_HPMax : INumericWatcher
    {
        /// <summary>
        /// 响应 MaxHP 数值变更
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"MaxHP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            unit.DynamicEvent(args).Coroutine();
        }
    }

    /// <summary>
    /// 客户端单位当前法力值（MP）变更监听器：派发 UI 动态事件刷新蓝条
    /// </summary>
    [NumericWatcher(SceneType.Current, NumericType.MP)]
    public class NumericWatcher_UnitInfo_MP : INumericWatcher
    {
        /// <summary>
        /// 响应 MP 数值变更
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"MP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            unit.DynamicEvent(args).Coroutine();
        }
    }

    /// <summary>
    /// 客户端单位最大法力值（MaxMP）变更监听器：派发 UI 动态事件刷新蓝条上限
    /// </summary>
    [NumericWatcher(SceneType.Current, NumericType.MaxMP)]
    public class NumericWatcher_UnitInfo_MPMax : INumericWatcher
    {
        /// <summary>
        /// 响应 MaxMP 数值变更
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"MaxMP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            unit.DynamicEvent(args).Coroutine();
        }
    }
}
