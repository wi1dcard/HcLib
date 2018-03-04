using System;
namespace HcHttp
{
	/// <summary>
	/// 回调管理类
	/// </summary>
	public class Callbacks
	{
		public enum Status
		{
			SendStart,
			SendFinish,
			RecvStart,
			RecvFinish
		}
		public Action<long, long, long> OnSending { get; set; }
		public Action<long, long, long> OnRecving { get; set; }
		public Action<Status, long> OnStatus { get; set; }

		public Callbacks()
		{
			this.OnStatus = (x, y) =>
			{
				//System.Diagnostics.Debug.WriteLine(x);
			};
		}
	}
}