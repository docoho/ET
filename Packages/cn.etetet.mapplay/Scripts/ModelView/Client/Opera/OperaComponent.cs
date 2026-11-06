using System;

using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 客户端场景操作与快捷键组件（如 R 键热重载）。
	/// </summary>
	[ComponentOf(typeof(Scene))]
	public class OperaComponent: Entity, IAwake, IUpdate
    {
        /// <summary>地面点击坐标点</summary>
        public Vector3 ClickPoint;

        /// <summary>Map 地面图层的 LayerMask</summary>
	    public int mapMask;
    }
}
