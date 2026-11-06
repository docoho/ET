#if UNITY_EDITOR

using System.Collections.Generic;

namespace YIUIFramework.Editor
{
    /// <summary>
    /// 红点Key引用关联检查结果数据（记录该Key是否已配置及被哪些子红点引用为父节点）。
    /// </summary>
    internal class RedDotLinkData
    {
        internal int Key;

        internal bool ConfigSet;

        internal List<int> LinkKey = new List<int>();
    }
}

#endif