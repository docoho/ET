using System;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 简易追踪目标物体的 MonoBehaviour 脚本，每帧以恒定速度朝目标 Transform 移动并正对目标
    /// </summary>
    [EnableClass]
    public class MoveToGameObject: MonoBehaviour
    {
        /// <summary>
        /// 追踪的目标 Transform
        /// </summary>
        public Transform Target;

        /// <summary>
        /// 移动速度（米/秒）
        /// </summary>
        public float Speed;
        
        /// <summary>
        /// 每帧朝目标位置匀速移动并更新朝向向量
        /// </summary>
        public void Update()
        {
            this.transform.position = Vector3.MoveTowards(transform.position, Target.position, Speed * Time.deltaTime);
            this.transform.forward = this.Target.position - this.transform.position;
        }
    }
}