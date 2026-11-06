using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ET.Client
{
    /// <summary>
    /// 客户端技能施法范围指示器表现组件系统（实时跟随鼠标射线在地面投影指示圈，并在左键点击时返回选定坐标）。
    /// </summary>
    [EntitySystemOf(typeof(SpellIndicatorComponent))]
    public static partial class SpellIndicatorComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this SpellIndicatorComponent self)
        {

        }

        /// <summary>
        /// 每帧检测鼠标在地面（Map 层）的射线交点，限制在最大施法距离内更新指示器位置，并在左键按下时完成坐标选择。
        /// </summary>
        [EntitySystem]
        private static void Update(this SpellIndicatorComponent self)
        {
            if (self.Current is null)
            {
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Map")))
            {
                float3 pos = hit.point;
                Unit unit = self.GetParent<Unit>();
                if (Vector3.Distance(unit.Position, pos) > self.MaxDistance / 1000f)
                {
                    pos = unit.Position + math.normalize(pos - unit.Position) * self.MaxDistance / 1000f;
                }

                self.Current.position = pos + new float3(0, 0.1f, 0);
            }
            
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector3 pos = self.Current.position;
                self.Current.gameObject.SetActive(false);
                self.Current.position = new Vector3(10000, 10000, 10000);
                self.Current.localScale = new Vector3(1, 1, 1);
                self.Current = null;
                self.Task?.SetResult(pos);
            }
        }

        /// <summary>
        /// 显示并缩放指定的施法指示器预制体实例，异步等待玩家点击确认施法位置。
        /// </summary>
        public static async ETTask<Vector3> WaitPos(this SpellIndicatorComponent self, GameObject go, int radius, int maxDistance)
        {
            self.Task = ETTask<Vector3>.Create();

            if (!self.Cache.TryGetValue(go.GetInstanceID(), out GameObject gameObject))
            {
                gameObject = UnityEngine.Object.Instantiate(go, GameObject.Find("/Global/Unit").transform);
                self.Cache.Add(go.GetInstanceID(), gameObject);
            }

            gameObject.SetActive(true);
            gameObject.transform.localScale *= radius / 1000f;
            self.Current = gameObject.transform;
            self.MaxDistance = maxDistance;
            
            return await self.Task;
        }

        /// <summary>
        /// 根据圆形目标选择器（<see cref="TargetSelectorCircle"/>）加载指示器资源并等待玩家选择坐标。
        /// </summary>
        public static async ETTask<Vector3> WaitSpellIndicator(this SpellIndicatorComponent self, TargetSelectorCircle targetSelectorCircle)
        {
            EntityRef<SpellIndicatorComponent> selfRef = self;
            GameObject spellIndicator = await self.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(targetSelectorCircle.SpellIndicatorName);
            self = selfRef;
            return await self.WaitPos(spellIndicator, targetSelectorCircle.Radius, targetSelectorCircle.MaxDistance);
        }
        
        /// <summary>
        /// 根据坐标点目标选择器（<see cref="TargetSelectorPosition"/>）加载指示器资源并等待玩家选择坐标。
        /// </summary>
        public static async ETTask<Vector3> WaitSpellIndicator(this SpellIndicatorComponent self, TargetSelectorPosition targetSelectorPosition)
        {
            EntityRef<SpellIndicatorComponent> selfRef = self;
            GameObject spellIndicator = await self.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(targetSelectorPosition.SpellIndicatorName);
            self = selfRef;
            return await self.WaitPos(spellIndicator, targetSelectorPosition.Radius, targetSelectorPosition.MaxDistance);
        }
    }
}
