using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 帧同步确定性逻辑帧更新调度器，保证所有注册了 <see cref="ILSUpdate"/> 的实体严格按实体 ID 升序执行更新
    /// </summary>
    public class LSUpdater: Object
    {
        /// <summary>
        /// 当前帧待遍历更新的实体 ID 有序列表
        /// </summary>
        private List<long> updateIds = new();

        /// <summary>
        /// 下一帧或新增待合并排序的实体 ID 列表
        /// </summary>
        private List<long> newUpdateIds = new();

        /// <summary>
        /// 已注册更新的帧同步实体弱引用字典（实体 ID -> EntityRef）
        /// </summary>
        private readonly Dictionary<long, EntityRef<LSEntity>> lsEntities = new();

        /// <summary>
        /// 执行单帧确定性更新：将新增实体合并并按 ID 升序排序后逐一调用 <see cref="LSEntitySystemSingleton.LSUpdate"/>，自动剔除已销毁实体
        /// </summary>
        public void Update()
        {
            if (this.newUpdateIds.Count > 0)
            {
                foreach (long id in this.newUpdateIds)
                {
                    this.updateIds.Add(id);
                }
                // 关键：按实体 ID 排序，确保多端每帧遍历实体的顺序完全一致
                this.updateIds.Sort();
                this.newUpdateIds.Clear();
            }

            foreach (long id in this.updateIds)
            {
                LSEntity entity = lsEntities[id];
                if (entity == null)
                {
                    this.lsEntities.Remove(id);
                    continue;
                }
                this.newUpdateIds.Add(id);
                LSEntitySystemSingleton.Instance.LSUpdate(entity);
            }
            this.updateIds.Clear();
            ObjectHelper.Swap(ref this.updateIds, ref this.newUpdateIds);
        }
        
        /// <summary>
        /// 将帧同步实体加入逻辑帧更新队列
        /// </summary>
        /// <param name="entity">待注册的帧同步实体</param>
        public void Add(LSEntity entity)
        {
            this.newUpdateIds.Add(entity.Id);
            this.lsEntities.Add(entity.Id, entity);
        }
    }
}