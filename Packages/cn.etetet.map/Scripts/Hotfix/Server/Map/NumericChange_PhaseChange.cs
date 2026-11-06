namespace ET.Server
{
    /// <summary>
    /// 服务端监听单位相位数值（<see cref="NumericType.Phase"/>）变化的监听器。
    /// </summary>
    [NumericWatcher(SceneType.Map, NumericType.Phase)]
    public class NumericChange_PhaseChange: INumericWatcher
    {
        /// <summary>
        /// 当单位相位改变时，通过移除并重新挂载 <see cref="AOIEntity"/> 刷新其在旧位面的离开视野与新位面的进入视野。
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            // Phase改变，将unit从aoi中移除，再重新加入aoi
            unit.RemoveComponent<AOIEntity>();
            unit.AddComponent<AOIEntity>();
        }
    }
}