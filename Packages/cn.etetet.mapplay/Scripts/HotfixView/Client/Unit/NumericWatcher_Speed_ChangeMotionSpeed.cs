namespace ET.Client
{
	/// <summary>
	/// 客户端监视speed数值变化，改变动画移动速度参数（MoveSpeed）。
	/// </summary>
	[NumericWatcher(SceneType.Current, NumericType.Speed)]
	public class NumericWatcher_Speed_ChangeMotionSpeed : INumericWatcher
	{
		public void Run(Unit unit, NumbericChange args)
		{
			unit.GetComponent<AnimatorComponent>().SetFloat(nameof(MotionType.MoveSpeed), args.New / 1000f);
		}
	}
}
