namespace ET
{
    /// <summary>
    /// 数值属性比较条件节点，由条件表达式编译器（如解析 "HP >= 100:200101"）或行为树配置生成。
    /// </summary>
    public class BTNumericCompare : BTCondition
    {
        /// <summary>
        /// 目标 Unit 在 BTEnv 中的键名（默认为 "Unit"）。
        /// </summary>
        public string OwnerKey;
        /// <summary>
        /// 要读取并比较的数值属性枚举类型。
        /// </summary>
        public NumericType NumericType;
        /// <summary>
        /// 比较运算符。
        /// </summary>
        public ConditionCompareOp Op;
        /// <summary>
        /// 比较右侧的目标常数值。
        /// </summary>
        public long Value;
        /// <summary>
        /// 比较不成立时返回的错误码。
        /// </summary>
        public int ErrorCode;
    }
}
