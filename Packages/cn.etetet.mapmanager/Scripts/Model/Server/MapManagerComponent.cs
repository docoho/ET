using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 单种地图模板管理实体（每种地图对应一个 <see cref="MapInfo"/>，其子实体为该地图下的多个 <see cref="MapCopy"/> 副本或分线）。
    /// </summary>
    // 每种地图一个MapInfo，MapInfo管理多个MapCopy
    [ChildOf(typeof(MapManagerComponent))]
    public class MapInfo: Entity, IAwake<string>
    {
        /// <summary>
        /// 地图配置名称（如 Map1、Map2）。
        /// </summary>
        public string MapName;
    }
    
    /// <summary>
    /// 地图副本或分线的运行状态枚举。
    /// </summary>
    public enum MapCopyStatus
    {
        /// <summary>
        /// 正常运行中，可接收新玩家进入。
        /// </summary>
        Running = 1,
        /// <summary>
        /// 等待结算结束。
        /// </summary>
        // 等待结束
        WaitFinish = 2,
        /// <summary>
        /// 已结束。
        /// </summary>
        // 结束
        Finished = 3,
        /// <summary>
        /// 等待合线中（暂停接收新玩家分配）。
        /// </summary>
        // 等待合线
        WaitMerge = 4,
    }
    
    /// <summary>
    /// 单个地图副本或分线实体（一个 <see cref="MapCopy"/> 对应一个独立的 <see cref="SceneType.Map"/> 纤程）。
    /// </summary>
    // 地图副本或者分线，一个MapCopy对应一个Fiber
    [ChildOf(typeof(MapInfo))]
    public class MapCopy: Entity, IAwake<long>, IDestroy
    {
        /// <summary>
        /// 当前已进入该副本的所有在线玩家 ID 集合。
        /// </summary>
        // 该副本已经进入的所有玩家
        public HashSet<long> Players = new();
        
        /// <summary>
        /// 已分配到该副本但尚在传送途中的等待玩家字典（Key 为玩家 ID，Value 为申请时间戳）。
        /// </summary>
        // 等待进入的玩家, key 为玩家id，value为进入副本的时间
        public Dictionary<long, long> WaitEnterPlayer = new();
        
        /// <summary>
        /// 副本当前状态。
        /// </summary>
        public MapCopyStatus Status = MapCopyStatus.Running;

        /// <summary>
        /// 最近一次完成合线的时间戳。
        /// </summary>
        public long MergeTime = 0;

        /// <summary>
        /// 该地图副本对应的运行纤程 ID。
        /// </summary>
        public long FiberId;
    }
    
    /// <summary>
    /// 全局地图副本管理组件（挂载在 MapManager 场景上，管理所有地图模板的 <see cref="MapInfo"/>）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MapManagerComponent: Entity, IAwake
    {
        /// <summary>
        /// 按地图名称索引的 <see cref="MapInfo"/> 实体引用字典。
        /// </summary>
        public Dictionary<string, EntityRef<MapInfo>> MapInfos = new();
    }
}
