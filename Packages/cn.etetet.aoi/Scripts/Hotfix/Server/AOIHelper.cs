using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// AOI 网格 ID 编码与视野范围网格计算辅助类。
    /// </summary>
    public static partial class AOIHelper
    {
        /// <summary>
        /// 将二维网格坐标 <c>(x, y)</c> 编码为 64 位整数 <c>cellId</c>（高 32 位存储 x，低 32 位存储 y）。
        /// </summary>
        public static long CreateCellId(int x, int y)
        {
            return (long) ((ulong) x << 32) | (uint) y;
        }

        /// <summary>
        /// 根据实体的 AOI 视野距离（<see cref="NumericType.AOI"/>）计算进入视野的网格集合（<paramref name="enterCell"/>）与离开视野的缓冲网格集合（<paramref name="leaveCell"/>，玩家向外多扩 1 圈防止边界抖动）。
        /// </summary>
        public static void CalcEnterAndLeaveCell(AOIEntity aoiEntity, int cellX, int cellY, HashSet<long> enterCell, HashSet<long> leaveCell)
        {
            enterCell.Clear();
            leaveCell.Clear();
            int viewDistance = aoiEntity.NumericComponent.Get(NumericType.AOI);
            if (viewDistance <= 0)
            {
                viewDistance = 1;
            }
            int r = (viewDistance - 1) / AOIManagerComponent.CellSize + 1;
            int leaveR = r;
            if (aoiEntity.Unit.UnitType == UnitType.Player)
            {
                leaveR += 1;
            }

            for (int i = cellX - leaveR; i <= cellX + leaveR; ++i)
            {
                for (int j = cellY - leaveR; j <= cellY + leaveR; ++j)
                {
                    long cellId = CreateCellId(i, j);
                    leaveCell.Add(cellId);

                    if (i > cellX + r || i < cellX - r || j > cellY + r || j < cellY - r)
                    {
                        continue;
                    }

                    enterCell.Add(cellId);
                }
            }
        }
    }
}