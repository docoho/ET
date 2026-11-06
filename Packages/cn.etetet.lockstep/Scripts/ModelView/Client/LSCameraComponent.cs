using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 帧同步战斗相机表现层组件（挂载在 <see cref="Room"/> 上），负责管理主摄像机跟随当前观察的 <see cref="LSUnitView"/>
	/// </summary>
	[ComponentOf(typeof(Room))]
	public class LSCameraComponent : Entity, IAwake, ILateUpdate
	{
		// 战斗摄像机
		private Camera camera;

		/// <summary>
		/// 摄像机的 Unity <see cref="UnityEngine.Transform"/> 引用
		/// </summary>
		public Transform Transform;

		/// <summary>
		/// 主摄像机引用（设置时自动同步缓存 <see cref="Transform"/>）
		/// </summary>
		public Camera Camera
		{
			get
			{
				return this.camera;
			}
			set
			{
				this.camera = value;
				this.Transform = this.camera.transform;
			}
		}

		private EntityRef<LSUnitView> unitView;

		/// <summary>
		/// 当前摄像机跟随的目标单位视图引用
		/// </summary>
		public LSUnitView MyUnitView
		{
			get
			{
				return this.unitView;
			}
			set
			{
				this.unitView = value;
			}
		}

		/// <summary>
		/// 录像回放模式下当前观察的玩家索引（按 Tab 键递增切换视角）
		/// </summary>
		public int index;
	}
}
