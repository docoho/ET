using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 角色骨骼绑点枚举（BindPoint）字典键比较器（避免枚举装箱开销）。
    /// </summary>
    [EnableClass]
    public class BindPointComparer : IEqualityComparer<BindPoint>
    {
        public bool Equals(BindPoint x, BindPoint y) => x == y;

        public int GetHashCode(BindPoint obj) => ((int)obj).GetHashCode();
    }

    
    /// <summary>
    /// 挂载在角色预制体 GameObject 上的骨骼绑点 MonoBehaviour 组件：通过 Odin 序列化保存各部位绑点（头顶、双手、受击点等）对应的 Transform。
    /// </summary>
    [EnableClass]
    [Serializable]
    public class BindPointComponent : SerializedMonoBehaviour
    {
        /// <summary>绑点类型枚举 -> 对应骨骼节点 Transform 的映射字典</summary>
        [ShowInInspector]
        [NonSerialized, OdinSerialize]
        public Dictionary<BindPoint, Transform> BindPoints = new(new BindPointComparer());

        /// <summary>
        /// 获取角色身上配置的全部骨骼绑点字典。
        /// </summary>
        public Dictionary<BindPoint, Transform> GetBindPoints()
        {
            return BindPoints;
        }
    }
}
