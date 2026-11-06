using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端 Unit 逻辑实体创建完成后的表现层事件处理器：异步加载并实例化角色 GameObject，挂载动画组件；若为主角则挂载相机与输入控制组件，否则创建头顶血条 UI。
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            EntityRef<Scene> sceneRef = scene;
            Unit unit = args.Unit;
            EntityRef<Unit> unitRef = unit;
            // Unit View层
            string assetsName = $"Packages/cn.etetet.map/Bundles/Units/{unit.Config().Name}.prefab";
            GameObject bundleGameObject = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(assetsName);

            scene = sceneRef;
            GlobalComponent globalComponent = scene.Root().GetComponent<GlobalComponent>();
            GameObject go = UnityEngine.Object.Instantiate(bundleGameObject, globalComponent.Unit, true);
            unit = unitRef;
            go.AddComponent<GameObjectEntityRef>().Entity = unit;
            go.transform.position = unit.Position;
            go.transform.rotation = unit.Rotation;

            {
                using var _ = await scene.Root().CoroutineLockComponent.Wait(CoroutineLockType.SceneChange, 0);
                GameObjectPosHelper.OnTerrain(go.transform);
            }

            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<AnimatorComponent>();
            
            if (scene.Root().GetComponent<PlayerComponent>().MyId == unit.Id)
            {
                unit.AddComponent<CinemachineComponent>();
                unit.AddComponent<InputSystemComponent>();
                unit.AddComponent<SpellIndicatorComponent>();
            }
            else
            {
                //临时做法 //不是玩家的Unit，添加一个血条显示
                //await YIUIFactory.InstantiateAsync<HPView3DComponent>(unit, go.transform);
                await YIUIFactory.InstantiateAsync<HPViewComponent>(scene, unit, scene.YIUIMgr().UICache);
            }
            
            await ETTask.CompletedTask;
        }
    }
}