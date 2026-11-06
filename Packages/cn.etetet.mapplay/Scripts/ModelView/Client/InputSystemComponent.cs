using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端触发技能施放的表现层事件参数。
    /// </summary>
    public struct OnSpellTrigger
    {
        /// <summary>施法主角 Unit 引用</summary>
        public EntityRef<Unit> Unit;
        /// <summary>要施放的技能配置表 ID</summary>
        public int SpellConfigId;
    }

    /// <summary>
    /// 客户端触发命令宠物攻击的表现层事件参数。
    /// </summary>
    public struct OnPetAttackTrigger
    {
        /// <summary>主人 Unit 引用</summary>
        public EntityRef<Unit> Unit;
    }
    
    /// <summary>
    /// 客户端主角输入控制组件：持有 Unity InputSystem 实例并处理移动、视角、选怪与施法输入。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public partial class InputSystemComponent: Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>Unity InputSystem 自动生成的输入动作封装实例</summary>
        public InputSystem InputSystem;
        /// <summary>上次触发连续移动指令的时间戳（用于移动指令节流）</summary>
        public long PressTime;

        /// <summary>关联的主角相机控制组件引用</summary>
        public EntityRef<CinemachineComponent> CinemachineComponent;

        /// <summary>主角当前是否处于跳跃状态</summary>
        public bool IsJumping;
    }
}
