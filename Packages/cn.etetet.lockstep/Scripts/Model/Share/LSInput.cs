using System;
using MemoryPack;

namespace ET
{
    /// <summary>
    /// 单个玩家在单个逻辑帧内的操作输入结构体（支持 MemoryPack 高效二进制序列化与值相等性对比）
    /// </summary>
    [MemoryPackable]
    public partial struct LSInput
    {
        /// <summary>
        /// 定点数二维移动方向向量
        /// </summary>
        [MemoryPackOrder(0)]
        public TrueSync.TSVector2 V;

        /// <summary>
        /// 技能或功能按键状态位掩码/编号
        /// </summary>
        [MemoryPackOrder(1)]
        public int Button;
        
        /// <summary>
        /// 判断两个帧输入结构体内容是否完全一致
        /// </summary>
        public bool Equals(LSInput other)
        {
            return this.V == other.V && this.Button == other.Button;
        }

        public override bool Equals(object obj)
        {
            return obj is LSInput other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.V, this.Button);
        }

        public static bool operator==(LSInput a, LSInput b)
        {
            if (a.V != b.V)
            {
                return false;
            }

            if (a.Button != b.Button)
            {
                return false;
            }

            return true;
        }

        public static bool operator !=(LSInput a, LSInput b)
        {
            return !(a == b);
        }
    }
}