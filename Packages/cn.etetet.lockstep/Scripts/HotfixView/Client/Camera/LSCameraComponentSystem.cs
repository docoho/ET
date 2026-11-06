using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 帧同步战斗相机跟随组件系统，负责在 LateUpdate 中跟随本地玩家单位视图，并在录像回放模式下支持按 Tab 键切换观察目标
	/// </summary>
	[EntitySystemOf(typeof(LSCameraComponent))]
	[FriendOf(typeof(LSCameraComponent))]
	public static partial class LSCameraComponentSystem
	{
		/// <summary>
		/// 初始化主摄像机引用并设置固定俯视角（绕 X 轴旋转 20 度）
		/// </summary>
		[EntitySystem]
		private static void Awake(this LSCameraComponent self)
		{
			self.Camera = Camera.main;
			self.Camera.transform.rotation = Quaternion.Euler(new Vector3(20, 0, 0));
		}
		
		/// <summary>
		/// 每帧在所有单位视图位置更新后，将摄像机保持在观察目标后上方固定偏移处
		/// </summary>
		[EntitySystem]
		private static void LateUpdate(this LSCameraComponent self)
		{
			// 摄像机每帧更新位置
			Room room = self.GetParent<Room>();
			if (room.IsReplay)
			{
				if (Input.GetKeyDown(KeyCode.Tab))
				{
					++self.index;
					self.MyUnitView = new LSUnitView();
				}
			}

			LSUnitView lsUnit = self.MyUnitView;
			if (lsUnit == null)
			{
				long id = room.IsReplay? room.PlayerIds[self.index % room.PlayerIds.Count] : room.GetParent<Scene>().GetComponent<PlayerComponent>().MyId;
				self.MyUnitView = room.GetComponent<LSUnitViewComponent>().GetChild<LSUnitView>(id);
			}

			if (lsUnit == null)
			{
				return;
			}

			Vector3 pos = lsUnit.Transform.position;
			self.Transform.position = new Vector3(pos.x, pos.y + 3, pos.z - 5);
		}
	}
}
