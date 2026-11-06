using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端 Unit 信息组装辅助工具类。
    /// </summary>
    public static partial class UnitHelper
    {
        /// <summary>
        /// 将服务端 Unit 的基础信息、当前移动路径点以及全量数值属性打包为网络协议结构 UnitInfo。
        /// </summary>
        public static UnitInfo CreateUnitInfo(Unit unit)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            NumericComponent nc = unit.NumericComponent;
            unitInfo.UnitId = unit.Id;
            unitInfo.ConfigId = unit.ConfigId;
            unitInfo.Type = (int)unit.UnitType;
            unitInfo.Position = unit.Position;
            unitInfo.Forward = unit.Forward;

            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            if (moveComponent != null)
            {
                if (!moveComponent.IsArrived())
                {
                    unitInfo.MoveInfo = MoveInfo.Create();
                    unitInfo.MoveInfo.Points.Add(unit.Position);
                    for (int i = moveComponent.N; i < moveComponent.Targets.Count; ++i)
                    {
                        float3 pos = moveComponent.Targets[i];
                        unitInfo.MoveInfo.Points.Add(pos);
                    }
                }
            }

            foreach ((NumericType key, long value) in nc.NumericDic)
            {
                unitInfo.KV.Add((int)key, value);
            }

            return unitInfo;
        }
    }
}
