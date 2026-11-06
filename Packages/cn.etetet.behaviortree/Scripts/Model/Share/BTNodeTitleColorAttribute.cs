using System;

namespace ET
{
    /// <summary>
    /// 行为树节点标题栏颜色特性，用于在行为树可视化编辑器中区分不同类别的节点（如协程节点、条件节点等）。
    /// </summary>
    [EnableClass]
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class BTNodeTitleColorAttribute : Attribute
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public BTNodeTitleColorAttribute(float r, float g, float b, float a = 1f)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }
    }
}