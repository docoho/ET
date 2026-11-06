using System.Collections.Generic;
using ET;

namespace YIUIFramework
{
    /// <summary>
    /// 红点管理器 —— 异步脏标记批量刷新分部类，合并同一时间窗口内的频繁红点计数修改以降低UI刷新开销。
    /// </summary>
    public partial class RedDotMgr
    {
        //已存标记
        private HashSet<RedDotData> m_DirtyData;

        //1秒内刷新多少次
        private const int m_RedDotDirtyFrame = 2;

        private int m_LoopId;

        //初始化异步脏标
        private void InitAsyncDirty()
        {
            m_DirtyData = new HashSet<RedDotData>();

            ET.EventSystem.Instance?.YIUIInvokeEntitySyncSafety(Entity, new YIUIInvokeEntity_CountDownAdd
            {
                TimerCallback = RedDotUpdateRefresh,
                TotalTime = 10000,
                Interval = 1f / m_RedDotDirtyFrame,
                StartCallback = true,
                Forever = true
            });
        }

        private void RedDotUpdateRefresh(double residuetime, double elapsetime, double totaltime)
        {
            RefreshDirtyCount();
        }

        private void DisposeDirty()
        {
            ET.EventSystem.Instance?.YIUIInvokeEntitySyncSafety(Entity, new YIUIInvokeEntity_CountDownRemove { TimerCallback = RedDotUpdateRefresh });
        }

        private bool TryDirtySetCount(RedDotData data, int count)
        {
            m_DirtyData.Remove(data);
            var result = data.TryDirtySetCount(count);
            if (result)
            {
                m_DirtyData.Add(data);
            }

            return result;
        }

        private void RefreshDirtyCount()
        {
            foreach (var data in m_DirtyData)
            {
                data.RefreshDirtyCount();
            }

            m_DirtyData.Clear();
        }
    }
}