using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ET.Client
{
    /// <summary>
    /// 客户端主角新输入系统（Unity InputSystem）组件系统：负责处理键盘 WASD 移动、鼠标右键视角旋转、左键射线点击选中目标、快捷键施法及宠物攻击指令。
    /// </summary>
    [EntitySystemOf(typeof(InputSystemComponent))]
    public static partial class InputSystemComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this InputSystemComponent self)
        {
            self.InputSystem?.Dispose();
        }
        
        [EntitySystem]
        private static void Awake(this InputSystemComponent self)
        {
            self.CinemachineComponent = self.GetParent<Unit>().GetComponent<CinemachineComponent>();

            self.InputSystem = new InputSystem();
            self.InputSystem.Player.Enable();

            self.InputSystem.Player.Look.performed += self.Look;
            self.InputSystem.Player.Jump.started += self.Jump;
            self.InputSystem.Player.SelectTarget.canceled += self.SelectTarget;
            self.InputSystem.Player.ChangeTarget.canceled += self.ChangeTarget;
            self.InputSystem.Player.Spell.started += self.CastSpell;
            self.InputSystem.Player.PetAttack.started += self.PetAttack;
        }

        [EntitySystem]
        private static void Update(this InputSystemComponent self)
        {
            // 按住方向键时每 100ms 采样一次相机朝向下的移动向量并发起寻路移动
            if (self.InputSystem.Player.Move.IsPressed())
            {
                if (self.GetSingleton<TimeInfo>().ClientNow() - self.PressTime > 100)
                {
                    self.PressTime = self.GetSingleton<TimeInfo>().ClientNow();
                    Vector2 v = self.InputSystem.Player.Move.ReadValue<Vector2>();
                    self.Move(v);
                }
            }
        }

        /// <summary>
        /// 按住鼠标右键拖动时旋转相机跟随视角。
        /// </summary>
        private static void Look(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            if (!Mouse.current.rightButton.isPressed)
            {
                return;
            }

            Vector2 v = context.ReadValue<Vector2>();
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            cinemachineComponent.RotationFollow(v);
        }

        /// <summary>
        /// 将二维移动输入转换到当前相机水平朝向坐标系，驱动主角向目标点移动。
        /// </summary>
        private static void Move(this InputSystemComponent self, Vector2 v)
        {
            if (self.IsJumping)
            {
                return;
            }

            if (v.magnitude < 0.001f)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            v = v.normalized * 2;

            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            Vector3 eulerAngles = cinemachineComponent.Follow.rotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;

            Vector3 rotV = Quaternion.Euler(eulerAngles) * new float3(v.x, 0, v.y);
            float3 targetPos = new float3(rotV) + unit.Position;

            unit.MoveToAsync(targetPos).Coroutine();
        }

        /// <summary>
        /// 鼠标点击屏幕时向 Unit 图层发射物理射线，若命中单位则发起点击交互请求。
        /// </summary>
        private static void SelectTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            const float maxDistance = 1000.0f;
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, maxDistance, LayerMask.GetMask("Unit")))
            {
                return;
            }
            GameObject clickedObject = hit.collider.gameObject;

            GameObjectEntityRef gameObjectEntityRef = clickedObject.GetComponent<GameObjectEntityRef>();
            if (gameObjectEntityRef is null)
            {
                return;
            }

            if (gameObjectEntityRef.Entity is not Unit targetUnit)
            {
                return;
            }
            
            UnitClickHelper.Click(self.Root(), targetUnit.Id).Coroutine();
        }

        private static void ChangeTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
        }

        private static void Jump(this InputSystemComponent self, InputAction.CallbackContext context)
        {
        }

        /// <summary>
        /// 触发宠物攻击按键时发布 OnPetAttackTrigger 事件。
        /// </summary>
        private static void PetAttack(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            EventSystem.Instance.Publish(self.Scene(), new OnPetAttackTrigger() { Unit = self.GetParent<Unit>() });
        }

        /// <summary>
        /// 按下数字快捷键（Digit1~）时计算技能配置 ID 并发布 OnSpellTrigger 事件。
        /// </summary>
        private static void CastSpell(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            if (context.control is not KeyControl keyControl)
            {
                return;
            }

            int spellConfigId = (keyControl.keyCode - Key.Digit1) * 10 + 100000;
            EventSystem.Instance.Publish(self.Scene(), new OnSpellTrigger() { Unit = self.GetParent<Unit>(), SpellConfigId = spellConfigId });
        }
    }
}
