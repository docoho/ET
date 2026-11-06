using System;

namespace ET
{
    /// <summary>
    /// 行为树节点输出参数特性，标记该 string 字段表示要写入到 BTEnv 黑板中的变量键名，并声明输出的变量类型供下游节点的 [BTInput] 匹配与校验。
    /// </summary>
    [EnableClass]
    public class BTOutput: Attribute
    {
        public Type Type { get; }

        public BTOutput(Type type)
        {
            this.Type = type;
        }
    }
}