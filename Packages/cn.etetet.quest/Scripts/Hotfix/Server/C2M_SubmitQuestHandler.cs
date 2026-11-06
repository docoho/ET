using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端处理客户端提交完成任务请求（C2M_SubmitQuest）的消息处理器：校验提交 NPC 距离与所有任务目标完成度后结算任务。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_SubmitQuestHandler : MessageLocationHandler<Unit, C2M_SubmitQuest, M2C_SubmitQuest>
    {
        protected override async ETTask Run(Unit unit, C2M_SubmitQuest request, M2C_SubmitQuest response)
        {
            // 获取QuestComponent
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            Quest quest = questComponent.GetQuest(request.QuestId);
            if (quest == null)
            {
                Log.Warning($"submit not found quest: {request.QuestId}");
                return;
            }


            QuestConfig questConfig = quest.GetConfig();
            
            // 判断NPC是否存在
            Unit npc = unit.GetParent<UnitComponent>().Get(questConfig.SubmitNPC);
            if (npc == null)
            {
                response.Error = TextConstDefine.Quest_NotFoundNPC;
                return;
            }
            
            // NPC距离
            if (math.distance(unit.Position, npc.Position) > 10f)
            {
                response.Error = TextConstDefine.Quest_NPCTooFar;
                return;
            }
            
            // 检查任务是否可以提交
            if (!quest.CanSubmit())
            {
                response.Error = TextConstDefine.Quest_NotFinish;
                return;
            }
            
            // 完成任务
            QuestHelper.SubmitQuest(unit, quest.Id);
            await ETTask.CompletedTask;
        }
    }
}