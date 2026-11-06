using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    /// <summary>
    /// 单个地图副本/分线实体（<see cref="MapCopy"/>）系统。
    /// </summary>
    [EntitySystemOf(typeof(MapCopy))]
    public static partial class MapCopySystem
    {
        /// <summary>
        /// 副本实体销毁时异步移除其对应的 Map 纤程。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this MapCopy self)
        {
            self.Fiber().RemoveFiber(self.FiberId).Coroutine();
        }

        /// <summary>
        /// 副本实体唤醒时记录其关联的 Map 纤程 ID。
        /// </summary>
        [EntitySystem]
        private static void Awake(this MapCopy self, long fiberId)
        {
            self.FiberId = fiberId;
        }

        /// <summary>
        /// 玩家完成跨场景传送后，将其从等待进入列表移入正式在线玩家集合。
        /// </summary>
        public static void AddPlayer(this MapCopy self, long playerId)
        {
            if (!self.WaitEnterPlayer.Remove(playerId))
            {
                throw new Exception($"player not in wait list: {playerId}");
            }
            self.Players.Add(playerId);
        }

        /// <summary>
        /// 在玩家开始传送前，将其加入等待进入列表并记录申请时间戳（防止在传送途中该分线被合并销毁）。
        /// </summary>
        public static void AddWaitPlayer(this MapCopy self, long playerId)
        {
            self.WaitEnterPlayer.Add(playerId, self.GetSingleton<TimeInfo>().ServerNow());
        }
    }

    /// <summary>
    /// 单种地图模板管理器（<see cref="MapInfo"/>）系统，负责按配置策略分配分线号、动态创建或销毁 <see cref="MapCopy"/> 纤程。
    /// </summary>
    [EntitySystemOf(typeof(MapInfo))]
    public static partial class MapInfoSystem
    {
        /// <summary>
        /// 初始化地图模板名称。
        /// </summary>
        [EntitySystem]
        private static void Awake(this MapInfo self, string mapName)
        {
            self.MapName = mapName;
        }

        /// <summary>
        /// 顺序查找当前未使用的最小分线编号（从 1 开始递增）。
        /// </summary>
        private static long GetNotUsedLineNumber(this MapInfo self)
        {
            long lineNum = 1;
            foreach (long k  in self.Children.Keys)
            {
                if (k == lineNum)
                {
                    lineNum++;
                    continue;
                }
                break;
            }
            return lineNum;
        }
        
        /// <summary>
        /// 停止并销毁指定 ID 的地图副本纤程及其 <see cref="MapCopy"/> 子实体。
        /// </summary>
        public static async ETTask RemoveCopy(this MapInfo self, long id)
        {
            MapCopy mapCopy = self.GetChild<MapCopy>(id);
            if (mapCopy == null)
            {
                return;
            }
            long fiberId = mapCopy.FiberId;
            EntityRef<MapInfo> selfRef = self;
            await self.Fiber().RemoveFiber(fiberId);
            self = selfRef;
            if (self == null)
            {
                return;
            }
            mapCopy = self.GetChild<MapCopy>(id);
            if (mapCopy == null)
            {
                return;
            }
            
            Log.Debug($"remove map copy: {self.MapName}:{mapCopy.Id}");
            
            self.RemoveChild(id);
        }

        /// <summary>
        /// 获取指定 ID 的地图副本；若 <paramref name="id"/> 为 0，则优先查找人数未满推荐上限（<see cref="MapConfig.RecommendPlayerNum"/>）的运行中副本，若均已满或不存在则动态创建新副本/分线。
        /// </summary>
        public static async ETTask<MapCopy> GetCopy(this MapInfo self, long id = 0)
        {
            MapConfig mapConfig = self.Fiber().GetSingleton<MapConfigCategory>().GetByName(self.MapName);
            string mapName = self.MapName;
            MapCopy mapCopy = null;
            if (id != 0)
            {
                mapCopy = self.GetChild<MapCopy>(id);
                if (mapCopy != null)
                {
                    Log.Debug($"get map copy: {mapName}:{id}");
                    return mapCopy;
                }

                return null;
            }
            
            foreach (var kv in self.Children)
            {
                MapCopy copy = (MapCopy)kv.Value;
                if (copy.Status != MapCopyStatus.Running)
                {
                    continue;
                }
                if (copy.Players.Count < mapConfig.RecommendPlayerNum)
                {
                    Log.Debug($"get map copy: {mapName}:{copy.Id}:");
                    return copy;
                }
            }

            long mapId = 0;
            switch (mapConfig.CopyType)
            {
                case CopyType.Normal:
                {
                    mapId = IdGenerater.Instance.GenerateId();
                    break;
                }
                case CopyType.Line:
                {
                    mapId = self.GetNotUsedLineNumber();
                    break;
                }
                case CopyType.Copy:
                {
                    mapId = IdGenerater.Instance.GenerateId();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            mapCopy = await self.AddChildWithIdAsync(mapId);
            
            Log.Debug($"get map copy: {mapName}:{mapId}");
            
            return mapCopy;
        }
        
        /// <summary>
        /// 在线程池调度器上动态创建新的 <see cref="SceneType.Map"/> 纤程，并创建关联的 <see cref="MapCopy"/> 子实体。
        /// </summary>
        private static async ETTask<MapCopy> AddChildWithIdAsync(this MapInfo self, long id)
        {
            EntityRef<MapInfo> selfRef = self;
            
            // 创建Copy Fiber
            long fiberId = await self.Fiber().CreateFiber(SchedulerType.ThreadPool, id, SceneType.Map, $"{self.MapName}@{id}");
            self = selfRef;
            return self.AddChildWithId<MapCopy, long>(id, fiberId);
        }
    }
    
    /// <summary>
    /// 全局地图副本管理组件（<see cref="MapManagerComponent"/>）系统。
    /// </summary>
    [EntitySystemOf(typeof(MapManagerComponent))]
    public static partial class MapManagerComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this MapManagerComponent self)
        {

        }

        /// <summary>
        /// 按地图名称注册新的 <see cref="MapInfo"/> 子实体（若为分线制地图则自动挂载 <see cref="MergeLinesComponent"/> 合线组件）。
        /// </summary>
        private static MapInfo AddMap(this MapManagerComponent self, string mapName)
        {
            MapConfig mapConfig = self.Fiber().GetSingleton<MapConfigCategory>().GetByName(mapName);
            MapInfo mapInfo = self.AddChild<MapInfo, string>(mapName);
            if (mapConfig.CopyType == CopyType.Line)
            {
                mapInfo.AddComponent<MergeLinesComponent>();
            }
            self.MapInfos.Add(mapName, mapInfo);
            return mapInfo;
        }

        /// <summary>
        /// 异步获取或创建指定地图名称与副本 ID 的 <see cref="MapCopy"/>。
        /// </summary>
        public static async ETTask<MapCopy> GetMapAsync(this MapManagerComponent self, string mapName, long id = 0)
        {
            if (id != 0)
            {
                MapCopy mapCopy = self.GetMap(mapName, id);
                if (mapCopy != null)
                {
                    return mapCopy;
                }
            }

            MapInfo mapInfo = null;
            if (!self.MapInfos.TryGetValue(mapName, out var mapInfoRef))
            {
                mapInfo = self.AddMap(mapName);
            }
            else
            {
                mapInfo = mapInfoRef;
            }

            return await mapInfo.GetCopy(id);
        }
        
        /// <summary>
        /// 同步查找已存在的指定地图名称与副本 ID 的 <see cref="MapCopy"/>。
        /// </summary>
        public static MapCopy GetMap(this MapManagerComponent self, string mapName, long mapId)
        {
            if (!self.MapInfos.TryGetValue(mapName, out EntityRef<MapInfo> mapInfoRef))
            {
                return null;
            }

            MapInfo mapInfo = mapInfoRef;
            MapCopy mapCopy = mapInfo.GetChild<MapCopy>(mapId);
            return mapCopy;
        }
    }
}
