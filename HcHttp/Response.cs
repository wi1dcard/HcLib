using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
namespace HcHttp
{
	/// <summary>
	/// 响应管理类
	/// </summary>
	public class Response : Object
	{
		/// <summary>
		/// 响应体原始数据
		/// </summary>
		public byte[] Raw
		{
			get;
			private set;
		}

		/// <summary>
		/// 响应体文本，只在第一次读取时进行解码并缓存。
		/// </summary>
		private string m_Text;
		public string Text
		{
			get
			{
				if (this.m_Text == null)
				{
					this.m_Text = this.GetText();
				}
				return m_Text;
			}
		}

		/// <summary>
		/// 响应头信息
		/// </summary>
		public WebHeaderCollection Header
		{
			get;
			private set;
		}

		/// <summary>
		/// 响应 HTTP Status Code（如：200）
		/// </summary>
		public HttpStatusCode StatusCode
		{
			get;
			private set;
		}

		/// <summary>
		/// 响应 HTTP Status Description（如：OK）
		/// </summary>
		public string StatusDescription
		{
			get;
			private set;
		}

		/// <summary>
		/// 实际请求方法
		/// </summary>
		public string Method
		{
			get;
			private set;
		}

		/// <summary>
		/// 实际响应Uri（跟随3xx）
		/// </summary>
		public Uri ResponseUri
		{
			get;
			private set;
		}

		/// <summary>
		/// 响应字符集
		/// </summary>
		public string Charset
		{
			get;
			private set;
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="Response"></param>
		/// <param name="Stream"></param>
		public Response(HttpWebResponse Response, MemoryStream Stream)
		{
			this.Header = Response.Headers;
			this.StatusCode = Response.StatusCode;
			this.StatusDescription = Response.StatusDescription;
			this.Method = Response.Method;
			this.ResponseUri = Response.ResponseUri;
			this.Raw = Stream.ToArray();
			this.Charset = Response.CharacterSet;
		}

		/// <summary>
		/// 将原始数据解码为字符串
		/// </summary>
		/// <returns></returns>
		private string GetText()
		{
			Encoding encoding = Encoding.UTF8;
			try
			{
				encoding = Encoding.GetEncoding(this.Charset);
			}
			catch
			{
				var meta = Regex.Match(Encoding.Default.GetString(this.Raw), "<meta[^<]*charset=\"?(.*?)\"", RegexOptions.IgnoreCase);
				if (meta != null && meta.Groups.Count > 0)
				{
					string charset = meta.Groups[1].Value.Trim();
					try
					{
						encoding = Encoding.GetEncoding(charset);
					}
					catch
					{
					}
				}
			}
			return encoding.GetString(this.Raw);
		}

		/// <summary>
		/// 等效于读取Text属性
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.Text;
		}

		/// <summary>
		/// 转字符串运算符隐性重写
		/// </summary>
		/// <param name="Manager"></param>
		public static implicit operator string(Response Manager)
		{
			return Manager.ToString();
		}
	}
}