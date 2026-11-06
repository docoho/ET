namespace ET
{
    /// <summary>
    /// 动作节点配置：增加或减少指定 Buff 的叠加层数（Stack）。
    /// </summary>
    public class BTBuffAddStack: BTAction
    {
        /// <summary>输入黑板变量名：要修改层数的 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>增减的层数值</summary>
        public int Value;
    }
}