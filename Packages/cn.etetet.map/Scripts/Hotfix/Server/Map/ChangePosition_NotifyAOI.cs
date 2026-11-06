using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 监听服务端 Map 场景中单位位置变化（<see cref="ChangePosition"/>）并驱动 AOI 网格更新的事件处理器。
    /// </summary>
    [Event(SceneType.Map)]
    public class ChangePosition_NotifyAOI: AEvent<Scene, ChangePosition>
    {
        /// <summary>
        /// 计算新旧坐标所属的 AOI 网格坐标，若跨越了网格边界则调用 <see cref="AOIManagerComponentSystem.Move(AOIManagerComponent,AOIEntity,int,int)"/> 更新视野。
        /// </summary>
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            float3 oldPos = args.OldPos;
            int oldCellX = (int) (oldPos.x * 1000) / AOIManagerComponent.CellSize;
            int oldCellY = (int) (oldPos.z * 1000) / AOIManagerComponent.CellSize;
            int newCellX = (int) (unit.Position.x * 1000) / AOIManagerComponent.CellSize;
            int newCellY = (int) (unit.Position.z * 1000) / AOIManagerComponent.CellSize;
            if (oldCellX == newCellX && oldCellY == newCellY)
            {
                return;
            }

            AOIEntity aoiEntity = unit.GetComponent<AOIEntity>();
            if (aoiEntity == null)
            {
                return;
            }

            unit.Scene().GetComponent<AOIManagerComponent>().Move(aoiEntity, newCellX, newCellY);
            await ETTask.CompletedTask;
        }
    }
}