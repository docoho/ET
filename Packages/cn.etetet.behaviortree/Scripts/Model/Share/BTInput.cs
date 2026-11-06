using System;

namespace ET
{
    /// <summary>
    /// 行为树节点输入参数特性，标记该 string 字段表示要从 BTEnv 黑板中读取的变量键名，并声明期望的变量类型以供编辑器下拉框筛选与保存前静态校验。
    /// </summary>
    [EnableClass]
    public class BTInput: Attribute
    {
        public Type Type { get; }

        /// <summary>
        /// 仅用于代码生成标记（例如方法参数标记为输入）。
        /// 行为树节点字段请使用带Type参数的构造函数。
        /// </summary>
        public BTInput()
        {
            this.Type = typeof(void);
        }

        public BTInput(Type type)
        {
            this.Type = type;
        }
    }
}
