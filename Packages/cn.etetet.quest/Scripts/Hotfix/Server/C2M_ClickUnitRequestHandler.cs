using System.Collections.Generic;

namespace ET.Server
{
	/// <summary>
	/// 服务端处理客户端点击单位（C2M_ClickUnitRequest）的消息处理器：将目标设为选中单位，并返回该 NPC 关联的可接/进行中/可提交任务列表。
	/// </summary>
	[MessageHandler(SceneType.Map)]
	public class C2M_ClickUnitRequestHandler : MessageLocationHandler<Unit, C2M_ClickUnitRequest, M2C_ClickUnitResponse>
	{
		protected override async ETTask Run(Unit unit, C2M_ClickUnitRequest request, M2C_ClickUnitResponse response)
		{
			await ETTask.CompletedTask;
			
			Unit target = unit.GetParent<UnitComponent>().Get(request.UnitId);
			if (target == null)
			{
				return;
			}

			unit.GetComponent<TargetComponent>().Unit = target;
			
			// 有可接任务则发送任务信息
			QuestComponent questComponent = unit.GetComponent<QuestComponent>();
			
            QuestConfigCategory questConfigCategory = unit.Fiber().GetSingleton<QuestConfigCategory>();
            HashSet<int> allIds = questConfigCategory.GetAllQuestsById(target.Id);

			if (allIds == null)
			{
				return;
			}
			
            HashSet<int> questIds = questConfigCategory.GetAcceptQuestsById(target.Id);
            HashSet<int> submitQuestIds = questConfigCategory.GetSubmitQuestsById(target.Id);
			
			foreach (int questId in allIds)
			{
				Quest acceptedQuest = questComponent.GetQuest(questId);
				if (acceptedQuest != null) // 已接任务
				{
					Show_QuestInfo questInfo = Show_QuestInfo.Create();
					questInfo.QuestId = questId;
					// 已接已完成
					if (submitQuestIds.Contains(questId))
					{
						questInfo.Status = (int)QuestStatus.Submited;
					}
					else
					{
						questInfo.Status = (int)QuestStatus.InProgress;	
					}
					response.questInfo.Add(questInfo);
				}
				else
				{
					if (!questIds.Contains(questId))
					{
						continue;
					}
					
					// 可接
					Show_QuestInfo questInfo = Show_QuestInfo.Create();
					questInfo.QuestId = questId;
					questInfo.Status = (int)QuestStatus.Available;

					response.questInfo.Add(questInfo);
				}
			}
		}
	}
}
