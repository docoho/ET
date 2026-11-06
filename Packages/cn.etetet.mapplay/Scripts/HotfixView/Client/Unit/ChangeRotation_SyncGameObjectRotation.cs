using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端逻辑层 Unit 旋转朝向变更事件的表现层处理器：同步更新角色 GameObject 的 rotation。
    /// </summary>
    [Event(SceneType.Current)]
    public class ChangeRotation_SyncGameObjectRotation: AEvent<Scene, ChangeRotation>
    {
        protected override async ETTask Run(Scene scene, ChangeRotation args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }

            Transform transform = gameObjectComponent.GameObject.transform;
            transform.rotation = unit.Rotation;
            await ETTask.CompletedTask;
        }
    }
}
