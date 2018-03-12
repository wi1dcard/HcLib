using System;
namespace HcHttp
{
	/// <summary>
	/// 传输（发送请求体/接收响应体）事件参数
	/// </summary>
	public class TransmitEventArgs : EventArgs
	{
		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="chunkBytes"></param>
		/// <param name="transmittedBytes"></param>
		/// <param name="totalBytes"></param>
		public TransmitEventArgs(long chunkBytes, long transmittedBytes, long totalBytes)
		{
			this.chunkBytes = chunkBytes;
			this.transmittedBytes = transmittedBytes;
			this.totalBytes = totalBytes;
		}

		/// <summary>
		/// 本次分片传输字节数
		/// </summary>
		public long chunkBytes { get; set; }
		
		/// <summary>
		/// 已传输字节数
		/// </summary>
		public long transmittedBytes { get; set; }

		/// <summary>
		/// 总预计传输字节数
		/// </summary>
		public long totalBytes { get; set; }
	}

	/// <summary>
	/// 状态事件参数
	/// </summary>
	public class StatusEventArgs : EventArgs
	{
		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="status"></param>
		/// <param name="totalBytes"></param>
		public StatusEventArgs(Status status, long totalBytes)
		{
			this.status = status;
			this.totalBytes = totalBytes;
		}

		/// <summary>
		/// 传输状态
		/// </summary>
		public enum Status
		{
			/// <summary>
			/// 开始发送请求体
			/// </summary>
			SendStart,
			/// <summary>
			/// 请求体发送完毕
			/// </summary>
			SendFinish,
			/// <summary>
			/// 开始接收响应体
			/// </summary>
			RecvStart,
			/// <summary>
			/// 响应体接收完毕
			/// </summary>
			RecvFinish
		}

		/// <summary>
		/// 传输状态
		/// </summary>
		public Status status;

		/// <summary>
		/// 总传输（或预计传输）字节数
		/// </summary>
		public long totalBytes { get; set; }
	}
}