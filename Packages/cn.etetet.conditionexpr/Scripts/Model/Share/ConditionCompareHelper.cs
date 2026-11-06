using System;

namespace ET
{
    /// <summary>
    /// 条件表达式通用数值比较辅助类。
    /// </summary>
    public static class ConditionCompareHelper
    {
        /// <summary>
        /// 根据指定的比较运算符判断左右两个长整型数值是否满足关系。
        /// </summary>
        public static bool Compare(long left, ConditionCompareOp op, long right)
        {
            return op switch
            {
                ConditionCompareOp.Greater => left > right,
                ConditionCompareOp.GreaterEqual => left >= right,
                ConditionCompareOp.Less => left < right,
                ConditionCompareOp.LessEqual => left <= right,
                ConditionCompareOp.Equal => left == right,
                ConditionCompareOp.NotEqual => left != right,
                _ => throw new Exception($"unknown condition compare op: {op}")
            };
        }
    }
}
