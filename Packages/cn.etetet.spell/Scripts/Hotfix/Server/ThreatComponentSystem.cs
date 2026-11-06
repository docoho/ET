using System.Linq;

namespace ET.Server
{
    /// <summary>
    /// 怪物仇恨列表管理组件系统（维护攻击者对当前怪物的仇恨值并提供最高/最低/随机仇恨目标查询）。
    /// </summary>
    [EntitySystemOf(typeof(ThreatComponent))]
    public static partial class ThreatComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this ThreatComponent self)
        {
        }
        
        /// <summary>
        /// 为指定单位 <paramref name="unit"/> 增加仇恨值 <paramref name="threat"/>。
        /// </summary>
        public static void AddThreat(this ThreatComponent self, Unit unit, int threat)
        {
            ThreatInfo threatInfo = self.GetChild<ThreatInfo>(unit.Id);
            if (threatInfo == null)
            {
                threatInfo = self.AddChild<ThreatInfo>();
                threatInfo.Unit = unit;
                threatInfo.Threat = threat;
            }
            else
            {
                threatInfo.Threat += threat;
            }
        }

        /// <summary>
        /// 移除指定单位 ID 的仇恨记录。
        /// </summary>
        public static void RemoveThreat(this ThreatComponent self, long unitId)
        {
            self.RemoveChild(unitId);
        }
        
        /// <summary>
        /// 获取当前仇恨列表中的目标数量。
        /// </summary>
        public static int GetCount(this ThreatComponent self)
        {
            return self.ChildrenCount();
        }

        /// <summary>
        /// 清空所有仇恨记录（例如怪物脱战归位时调用）。
        /// </summary>
        public static void ClearThreat(this ThreatComponent self)
        {
            foreach (long unitId in self.Children.Keys.ToArray())
            {
                self.RemoveChild(unitId);
            }
        }

        /// <summary>
        /// 获取指定单位 ID 当前的仇恨值。
        /// </summary>
        public static int GetThreat(this ThreatComponent self, long unitId)
        {
            ThreatInfo threatInfo = self.GetChild<ThreatInfo>(unitId);
            if (threatInfo == null)
            {
                return 0;
            }
            return threatInfo.Threat;
        }

        /// <summary>
        /// 获取当前仇恨值最高的目标信息（自动清理已销毁的失效单位引用）。
        /// </summary>
        public static ThreatInfo GetMaxThreat(this ThreatComponent self)
        {
            int max = 0;
            ThreatInfo threatInfo = null;
            foreach (var kv in self.Children)
            {
                threatInfo = kv.Value as ThreatInfo;
                if (threatInfo.Unit.Entity == null)
                {
                    threatInfo = null;
                    self.RemoveThreat(kv.Key);
                    continue;
                }

                if (threatInfo.Threat <= max)
                {
                    continue;
                }

                max = threatInfo.Threat;
            }
            return threatInfo;
        }

        /// <summary>
        /// 获取当前仇恨值最低的目标信息。
        /// </summary>
        public static ThreatInfo GetMinThreat(this ThreatComponent self)
        {            
            int min = 0;
            ThreatInfo threatInfo = null;
            foreach (var kv in self.Children)
            {
                threatInfo = kv.Value as ThreatInfo;
                if (threatInfo.Unit.Entity == null)
                {
                    threatInfo = null;
                    self.RemoveThreat(kv.Key);
                    continue;
                }

                if (threatInfo.Threat >= min)
                {
                    continue;
                }

                min = threatInfo.Threat;
            }
            return threatInfo;
        }

        /// <summary>
        /// 从当前仇恨列表中随机选取一个目标信息。
        /// </summary>
        public static ThreatInfo GetRandomThreat(this ThreatComponent self)
        {
            int index = RandomGenerator.RandomNumber(0, self.Children.Count);
            int i = 0;
            ThreatInfo threatInfo = null;
            foreach (var kv in self.Children)
            {
                if (i == index)
                {
                    threatInfo = kv.Value as ThreatInfo;
                    break;
                }

                i++;
            }
            return threatInfo;
        }
    }
}
