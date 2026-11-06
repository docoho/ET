using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 单位 AOI（Area of Interest，感兴趣区域/视野）组件，记录单位当前所在的网格、订阅的进出网格集合以及双向可见实体列表。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class AOIEntity: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 单位当前所处的位面/相位标记（支持按位与匹配多相位共视）。
        /// </summary>
        public PhaseType Phase = PhaseType.Normal;

        /// <summary>
        /// 所属的父 <see cref="ET.Unit"/> 实体。
        /// </summary>
        public Unit Unit => this.GetParent<Unit>();

        private EntityRef<NumericComponent> numericComponent;

        /// <summary>
        /// 关联的数值组件引用（用于读取 AOI 视野半径与 Phase 属性）。
        /// </summary>
        public NumericComponent NumericComponent
        {
            get
            {
                return this.numericComponent;
            }
            set
            {
                this.numericComponent = value;
            }
        }

        private EntityRef<Cell> cell;

        /// <summary>
        /// 单位当前中心点所在的网格单元。
        /// </summary>
        public Cell Cell
        {
            get
            {
                return this.cell;
            }
            set
            {
                this.cell = value;
            }
        }

        /// <summary>
        /// 当前订阅了“进入视野”通知的网格 ID 集合。
        /// </summary>
        // 观察进入视野的Cell
        public HashSet<long> SubEnterCells = new HashSet<long>();

        /// <summary>
        /// 当前订阅了“离开视野”通知的网格 ID 集合（玩家通常比 Enter 多一圈防抖）。
        /// </summary>
        // 观察离开视野的Cell
        public HashSet<long> SubLeaveCells = new HashSet<long>();
        
        /// <summary>
        /// 跨网格移动时用于计算新进入网格差集的临时复用集合。
        /// </summary>
        // 观察进入视野的Cell
        public HashSet<long> enterHashSet = new HashSet<long>();

        /// <summary>
        /// 跨网格移动时用于计算新离开网格差集的临时复用集合。
        /// </summary>
        // 观察离开视野的Cell
        public HashSet<long> leaveHashSet = new HashSet<long>();

        /// <summary>
        /// 我看得见的所有单位字典（Key 为实体 Id）。
        /// </summary>
        // 我看的见的Unit
        public Dictionary<long, EntityRef<AOIEntity>> SeeUnits = new Dictionary<long, EntityRef<AOIEntity>>();
        
        /// <summary>
        /// 看得见我的所有单位字典（Key 为实体 Id）。
        /// </summary>
        // 看见我的Unit
        public Dictionary<long, EntityRef<AOIEntity>> BeSeeUnits = new Dictionary<long, EntityRef<AOIEntity>>();
        
        /// <summary>
        /// 我看得见的玩家单位字典（Key 为实体 Id）。
        /// </summary>
        // 我看的见的Player
        public Dictionary<long, EntityRef<AOIEntity>> SeePlayers = new Dictionary<long, EntityRef<AOIEntity>>();

        /// <summary>
        /// 看得见我的玩家单位字典（单独维护，专门用于将自身状态变更高效广播给周围玩家客户端）。
        /// </summary>
        // 看见我的Player单独放一个Dict，用于广播
        public Dictionary<long, EntityRef<AOIEntity>> BeSeePlayers = new Dictionary<long, EntityRef<AOIEntity>>();
    }
}