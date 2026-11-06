using System;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 目标 Map 场景接收跨场景传送 Unit（M2M_UnitTransferRequest）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class M2M_UnitTransferRequestHandler: MessageHandler<Scene, M2M_UnitTransferRequest, M2M_UnitTransferResponse>
    {
        /// <summary>
        /// 在目标 Map 中还原或直接挂载 Unit 及其跨场景组件，重新挂载移动、寻路、邮箱与 AOI 组件，并通知客户端切换场景。
        /// </summary>
        protected override async ETTask Run(Scene scene, M2M_UnitTransferRequest request, M2M_UnitTransferResponse response)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();

            Unit unit = request.Unit;
            if (unit != null)  // 黑科技，直接传送Unit对象
            {
                unitComponent.AddChild(unit);
                unitComponent.Add(unit);
            }
            else
            {
                unit = MongoHelper.Deserialize<Unit>(request.UnitBytes);

                unitComponent.AddChild(unit);
                unitComponent.Add(unit);

                foreach (byte[] bytes in request.EntityBytes)
                {
                    Entity entity = MongoHelper.Deserialize<Entity>(bytes);
                    unit.AddComponent(entity);
                }
            }

            // 重新挂载当前地图场景所需的运行时组件
            unit.AddComponent<TurnComponent>();
            unit.AddComponent<MoveComponent>();
            unit.AddComponent<PathfindingComponent, string>(scene.Name.GetSceneConfigName());
            unit.AddComponent<MailBoxComponent, int>(MailBoxType.OrderedMessage);
            unit.AddComponent<TargetComponent>();

            // 通知客户端开始切场景
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneId = scene.Id;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.NoticeClient(unit, m2CStartSceneChange, NoticeType.Self);

            if (request.ChangeScene)
            {
                // 通知客户端创建My Unit
                M2C_CreateMyUnit m2CCreateUnits = M2C_CreateMyUnit.Create();
                m2CCreateUnits.Unit = UnitHelper.CreateUnitInfo(unit);
                MapMessageHelper.NoticeClient(unit, m2CCreateUnits, NoticeType.Self);
            }

            // 加入aoi
            unit.AddComponent<AOIEntity>();

            // 返回 Unit 在新场景中的 ActorId，供源场景解锁 Location
            response.NewActorId = unit.GetActorId();
            await ETTask.CompletedTask;
        }
    }
}