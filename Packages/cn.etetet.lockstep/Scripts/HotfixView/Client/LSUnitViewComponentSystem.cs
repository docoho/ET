using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 帧同步单位视图管理组件系统，负责为 <see cref="LSWorld"/> 中的所有逻辑单位加载并实例化对应的 Unity 角色预制体与 <see cref="LSUnitView"/>
    /// </summary>
    [EntitySystemOf(typeof(LSUnitViewComponent))]
    public static partial class LSUnitViewComponentSystem
    {
        /// <summary>
        /// 组件初始化回调
        /// </summary>
        [EntitySystem]
        private static void Awake(this LSUnitViewComponent self)
        {

        }
        
        /// <summary>
        /// 组件销毁回调
        /// </summary>
        [EntitySystem]
        private static void Destroy(this LSUnitViewComponent self)
        {

        }

        /// <summary>
        /// 异步遍历房间内所有参赛玩家对应的 <see cref="LSUnit"/>，加载角色模型 Prefab 并创建绑定的 <see cref="LSUnitView"/> 与 <see cref="LSAnimatorComponent"/>
        /// </summary>
        /// <param name="self">帧同步单位视图管理组件实例</param>
        public static async ETTask InitAsync(this LSUnitViewComponent self)
        {
            Room room = self.Room();
            EntityRef<Room> roomRef = room;
            LSUnitComponent lsUnitComponent = room.LSWorld.GetComponent<LSUnitComponent>();
            Scene root = self.Root();
            foreach (long playerId in room.PlayerIds)
            {
                LSUnit lsUnit = lsUnitComponent.GetChild<LSUnit>(playerId);
                string assetsName = $"Packages/cn.etetet.demores/Bundles/Unit/Unit.prefab";
                room = roomRef;
                GameObject bundleGameObject = await room.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(assetsName);
                GameObject prefab = bundleGameObject.Get<GameObject>("Skeleton");

                GlobalComponent globalComponent = root.GetComponent<GlobalComponent>();
                GameObject unitGo = UnityEngine.Object.Instantiate(prefab, globalComponent.Unit, true);
                unitGo.transform.position = lsUnit.Position.ToVector();

                LSUnitView lsUnitView = self.AddChildWithId<LSUnitView, GameObject>(lsUnit.Id, unitGo);
                lsUnitView.AddComponent<LSAnimatorComponent>();
            }
        }
    }
}