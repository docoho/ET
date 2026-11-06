namespace YIUIFramework
{
    /// <summary>
    /// 红点级联传播链中的源头变更记录。
    /// 在整个修改堆栈中，由于父子红点数据是连锁聚合的，该类专门记录最初发起本次变更的叶子/源头红点快照。
    /// </summary>
    public class FirstRedDotChangeData
    {
        /// <summary>
        /// 第一个改变的数据
        /// </summary>
        public RedDotData ChangeData { get; internal set; }

        /// <summary>
        /// 第一个改变的 本来数量
        /// </summary>
        public int OriginalCount { get; internal set; }

        /// <summary>
        /// 第一个改变的 改变数量
        /// </summary>
        public int ChangeCount { get; internal set; }

        /// <summary>
        /// 第一个改变的 当前是否提示
        /// </summary>
        public bool ChangeTips { get; internal set; }

        internal FirstRedDotChangeData()
        {
        }

        public FirstRedDotChangeData(RedDotData changeData,
                                     int        originalCount,
                                     int        changeCount,
                                     bool       changeTips)
        {
            ChangeData    = changeData;
            OriginalCount = originalCount;
            ChangeCount   = changeCount;
            ChangeTips    = changeTips;
        }
    }
}