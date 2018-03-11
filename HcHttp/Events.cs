using System;
namespace HcHttp
{
	public class TransmitEventArgs : EventArgs
	{
		public TransmitEventArgs(long chunkBytes, long transmittedBytes, long totalBytes)
		{
			this.chunkBytes = chunkBytes;
			this.transmittedBytes = transmittedBytes;
			this.totalBytes = totalBytes;
		}

		public long chunkBytes { get; set; }
		
		public long transmittedBytes { get; set; }

		public long totalBytes { get; set; }
	}

	public class StatusEventArgs : EventArgs
	{
		public StatusEventArgs(Status status, long totalBytes)
		{
			this.status = status;
			this.totalBytes = totalBytes;
		}

		public enum Status
		{
			SendStart,
			SendFinish,
			RecvStart,
			RecvFinish
		}

		public Status status;

		public long totalBytes { get; set; }
	}
}