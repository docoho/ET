using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 单个逻辑帧内房间所有玩家的操作输入集合扩展部分（支持深拷贝 <see cref="CopyTo"/> 与预测帧相等性比对）
    /// </summary>
    public partial class OneFrameInputs
    {
        /// <summary>
        /// 比较两个单帧输入集合是否相等
        /// </summary>
        protected bool Equals(OneFrameInputs other)
        {
            return Equals(this.Inputs, other.Inputs);
        }

        /// <summary>
        /// 将当前帧的所有玩家输入深拷贝到目标 <see cref="OneFrameInputs"/> 实例中
        /// </summary>
        /// <param name="to">拷贝目标对象</param>
        public void CopyTo(OneFrameInputs to)
        {
            to.Inputs.Clear();
            foreach (var kv in this.Inputs)
            {
                to.Inputs.Add(kv.Key, kv.Value);
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((OneFrameInputs) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Inputs);
        }

        public static bool operator==(OneFrameInputs a, OneFrameInputs b)
        {
            if (a is null || b is null)
            {
                if (a is null && b is null)
                {
                    return true;
                }
                return false;
            }
            
            if (a.Inputs.Count != b.Inputs.Count)
            {
                return false;
            }

            foreach (var kv in a.Inputs)
            {
                if (!b.Inputs.TryGetValue(kv.Key, out LSInput inputInfo))
                {
                    return false;
                }

                if (kv.Value != inputInfo)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(OneFrameInputs a, OneFrameInputs b)
        {
            return !(a == b);
        }
    }
}