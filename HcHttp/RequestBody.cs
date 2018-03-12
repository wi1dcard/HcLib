using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HcHttp.RequestBody
{
	/// <summary>
	/// 二进制请求体
	/// </summary>
	public class Binary : IBody
	{
		public Binary()
		{
			this.ContentType = "application/octet-stream";
			this.Raw = new byte[0];
		}

		public Binary(string Content) : this()
		{
			this.ContentType = "text/plain";
			this.Content = Content;
		}

		public Binary(byte[] Raw) : this()
		{
			this.Raw = Raw;
		}

		public byte[] Raw { get; set; }

		public string ContentType { get; set; }

		public int ContentLength
		{
			get
			{
				return Raw.Length;
			}
		}

		public string Content
		{
			get
			{
				return Encoding.UTF8.GetString(this.Raw);
			}

			set
			{
				this.Raw = Encoding.UTF8.GetBytes(value);
			}
		}

		public override string ToString()
		{
			return this.Content;
		}

	}

	/// <summary>
	/// Url Encoded 请求体
	/// </summary>
	public class FormUrlEncoded : Dictionary<string, string>, IBody
	{
		public FormUrlEncoded() { }

		public byte[] Raw
		{
			get
			{
				return Encoding.UTF8.GetBytes(this.Content);
			}
		}

		public string ContentType
		{
			get
			{
				return "application/x-www-form-urlencoded";
			}
		}

		public int ContentLength
		{
			get
			{
				return Raw.Length;
			}
		}

		public string Content
		{
			get
			{
				if (base.Count == 0)
				{
					return "";
				}
				StringBuilder Data = new StringBuilder(base.Count * 64);
				foreach (var Kv in this)
				{
					Data.Append("&");
					Data.Append(Kv.Key);
					Data.Append("=");
					Data.Append(Uri.EscapeDataString(Kv.Value));
				}
				Data.Remove(0, 1);
				return Data.ToString();
			}

			set
			{
				string[] Params = value.Split('&');
				foreach (string Param in Params)
				{
					string[] Kv = Param.Split('=');
					if (Kv.Count() == 2)
					{
						base.Add(Kv[0], Uri.UnescapeDataString(Kv[1]));
					}
				}
			}
		}

		public override string ToString()
		{
			return this.Content;
		}

	}

	/// <summary>
	/// Form Data 请求体
	/// </summary>
	public class FormData : Dictionary<string, object>, IBody
	{
		private string Boundary;

		public FormData()
		{
			this.Boundary = Guid.NewGuid().ToString("N");
		}

		public int ContentLength
		{
			get
			{
				return Raw.Length;
			}
		}

		public string ContentType
		{
			get
			{
				return "multipart/form-data; boundary=" + this.Boundary;
			}
		}

		public byte[] Raw
		{
			get
			{
				var Stream = new MemoryStream();
				var StreamWriter = new StreamWriter(Stream, Encoding.UTF8)
				{
					AutoFlush = true
				};
				Stream.Position = 0;
				foreach (var Kv in this)
				{
					StreamWriter.Write("--" + Boundary + "\r\n");
					if (Kv.Value is File)
					{
						File File = Kv.Value as File;
						if (File.Stream != null)
						{
							StreamWriter.Write("Content-Disposition: form-data; name=\"" + Kv.Key + "\"; filename=\"" + File.FileName + "\"\r\n");
							StreamWriter.Write("Content-Type: application/octet-stream\r\n");
							StreamWriter.Write("\r\n");
							var Pos = File.Stream.Position;
							File.Stream.CopyTo(Stream);
							File.Stream.Position = Pos;
						}
					}
					else
					{
						StreamWriter.Write("Content-Disposition: form-data; name=\"" + Kv.Key + "\"\r\n");
						StreamWriter.Write("\r\n");
						StreamWriter.Write(Kv.Value.ToString());
					}
					StreamWriter.Write("\r\n");
				}
				StreamWriter.Write("--" + Boundary + "--\r\n");
				return Stream.ToArray();
			}
		}

		public class File
		{
			public string FileName { get; set; }

			public Stream Stream { get; private set; }

			public string FilePath { get; private set; }

			public File(Stream Stream)
			{
				this.Stream = Stream;
			}

			public File(string FilePath)
			{
				this.FilePath = FilePath;
				this.FileName = new FileInfo(FilePath).Name;
				this.Stream = System.IO.File.OpenRead(FilePath);
			}
		}

	}

	/// <summary>
	/// 请求体抽象接口
	/// </summary>
	public interface IBody
	{
		/// <summary>
		/// 请求体原始二进制数据
		/// </summary>
		byte[] Raw { get; }
		/// <summary>
		/// 此请求体对应ContentType值
		/// </summary>
		string ContentType { get; }
		/// <summary>
		/// 此请求体ContentLength值
		/// </summary>
		int ContentLength { get; }
	}
}