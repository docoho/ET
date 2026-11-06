namespace ET
{
    /// <summary>
    /// 动作节点配置：增加或减少指定 Buff 的剩余持续时间（修改其过期时间并重置过期定时器）。
    /// </summary>
    public class BTBuffAddExpireTime: BTAction
    {
        /// <summary>输入黑板变量名：要调整持续时间的 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>固定增减时长（毫秒）</summary>
        public int Value;

        /// <summary>百分比增减比例</summary>
        public int Pct;
    }
}