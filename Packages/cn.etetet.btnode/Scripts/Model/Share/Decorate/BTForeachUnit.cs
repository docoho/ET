using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 装饰器节点配置：遍历黑板中的单位 ID 列表（List&lt;long&gt;），依次查找存活的 Unit 实例写入输出黑板变量，并对每个单位执行一次子节点。
    /// </summary>
    public class BTForeachUnit: BTDecorate
    {
        /// <summary>输入黑板变量名：待遍历的目标单位 ID 列表（List&lt;long&gt;）</summary>
        [BTInput(typeof(List<long>))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Units;
        
        /// <summary>输出黑板变量名：当前循环迭代出的单个 Unit 实例</summary>
        [BTOutput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit;
    }
}