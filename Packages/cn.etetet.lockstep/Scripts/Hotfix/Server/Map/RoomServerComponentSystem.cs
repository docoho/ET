using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 帧同步房间服务端管理组件系统，负责维护房间内参赛玩家实体 <see cref="RoomPlayer"/> 及加载进度检查
    /// </summary>
    [EntitySystemOf(typeof(RoomServerComponent))]
    [FriendOf(typeof(RoomServerComponent))]
    public static partial class RoomServerComponentSystem
    {
        /// <summary>
        /// 根据匹配成功的玩家 ID 列表创建对应的 <see cref="RoomPlayer"/> 子实体
        /// </summary>
        /// <param name="self">房间服务端管理组件实例</param>
        /// <param name="playerIds">参赛玩家 ID 列表</param>
        [EntitySystem]
        private static void Awake(this RoomServerComponent self, List<long> playerIds)
        {
            foreach (long id in playerIds)
            {
                RoomPlayer roomPlayer = self.AddChildWithId<RoomPlayer>(id);
            }
        }

        /// <summary>
        /// 检查房间内所有玩家的场景加载进度是否均已达到 100%
        /// </summary>
        /// <param name="self">房间服务端管理组件实例</param>
        /// <returns>若全部玩家加载完成则返回 true，否则返回 false</returns>
        public static bool IsAllPlayerProgress100(this RoomServerComponent self)
        {
            foreach (RoomPlayer roomPlayer in self.Children.Values)
            {
                if (roomPlayer.Progress != 100)
                {
                    return false;
                }
            }
            return true;
        }
    }
}