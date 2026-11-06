using TrueSync;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端操作输入采样组件系统，每帧读取玩家 WASD 键盘输入并转换为归一化定点数二维向量写入 <see cref="LSClientUpdater.Input"/>
    /// </summary>
    [EntitySystemOf(typeof(LSOperaComponent))]
    [FriendOf(typeof(LSClientUpdater))]
    public static partial class LSOperaComponentSystem
    {
        /// <summary>
        /// 组件初始化回调
        /// </summary>
        [EntitySystem]
        private static void Awake(this LSOperaComponent self)
        {

        }
        
        /// <summary>
        /// 每帧采样 WASD 按键状态，合成定点数移动方向向量 <see cref="TSVector2"/> 并同步给 <see cref="LSClientUpdater"/>
        /// </summary>
        [EntitySystem]
        private static void Update(this LSOperaComponent self)
        {
            TSVector2 v = new();
            if (Input.GetKey(KeyCode.W))
            {
                v.y += 1;
            }

            if (Input.GetKey(KeyCode.A))
            {
                v.x -= 1;
            }

            if (Input.GetKey(KeyCode.S))
            {
                v.y -= 1;
            }

            if (Input.GetKey(KeyCode.D))
            {
                v.x += 1;
            }

            LSClientUpdater lsClientUpdater = self.GetParent<Room>().GetComponent<LSClientUpdater>();
            lsClientUpdater.Input.V = v.normalized;
        }

    }
}