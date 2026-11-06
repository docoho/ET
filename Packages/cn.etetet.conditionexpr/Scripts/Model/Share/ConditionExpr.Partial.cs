namespace ET
{
    /// <summary>
    /// Luban 配置表中 ConditionExpr 类型的分部扩展类，在配置表初始化完成（EndInit）时自动将文本表达式编译为行为树根节点 ConditionRoot。
    /// </summary>
    public sealed partial class ConditionExpr
    {
        /// <summary>
        /// 编译生成的条件行为树根节点缓存。
        /// </summary>
        public ConditionRoot Root { get; private set; }

        partial void EndInit()
        {
            this.Root = ConditionExprCompiler.Compile(this.Expr, this.ErrorCode);
        }
    }
}
