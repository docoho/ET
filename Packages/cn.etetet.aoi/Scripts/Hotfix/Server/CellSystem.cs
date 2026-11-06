using System.Collections.Generic;
using System.Text;

namespace ET.Server
{
    /// <summary>
    /// AOI 单个网格单元（<see cref="Cell"/>）系统。
    /// </summary>
    [EntitySystemOf(typeof(Cell))]
    public static partial class CellSystem
    {
        /// <summary>
        /// 网格单元唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this ET.Server.Cell self)
        {

        }
        
        /// <summary>
        /// 网格单元销毁时清空内部实体与订阅字典。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this Cell self)
        {
            self.AOIUnits.Clear();

            self.SubsEnterEntities.Clear();

            self.SubsLeaveEntities.Clear();
        }

        /// <summary>
        /// 将 <see cref="AOIEntity"/> 加入当前网格。
        /// </summary>
        public static void Add(this Cell self, AOIEntity aoiEntity)
        {
            self.AOIUnits.Add(aoiEntity.Id, aoiEntity);
        }

        /// <summary>
        /// 将 <see cref="AOIEntity"/> 从当前网格移除。
        /// </summary>
        public static void Remove(this Cell self, AOIEntity aoiEntity)
        {
            self.AOIUnits.Remove(aoiEntity.Id);
        }

        /// <summary>
        /// 将 64 位 <paramref name="cellId"/> 解码格式化为 <c>"x:y"</c> 字符串（用于调试日志）。
        /// </summary>
        public static string CellIdToString(this long cellId)
        {
            int y = (int)(cellId & 0xffffffff);
            int x = (int)((ulong)cellId >> 32);
            return $"{x}:{y}";
        }

        /// <summary>
        /// 将一组 <paramref name="cellIds"/> 批量格式化为逗号分隔的 <c>"x:y,"</c> 字符串。
        /// </summary>
        public static string CellIdToString(this HashSet<long> cellIds)
        {
            StringBuilder sb = new StringBuilder();
            foreach (long cellId in cellIds)
            {
                sb.Append(cellId.CellIdToString());
                sb.Append(",");
            }

            return sb.ToString();
        }

    }
}