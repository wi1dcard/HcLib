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

		public WebHeaderCollection Header
		{
			get;
			private set;
		}

		public HttpStatusCode StatusCode
		{
			get;
			private set;
		}

		public string StatusDescription
		{
			get;
			private set;
		}

		public string Method
		{
			get;
			private set;
		}

		public Uri ResponseUri
		{
			get;
			private set;
		}

		public string Charset
		{
			get;
			private set;
		}

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

		public override string ToString()
		{
			return this.Text;
		}

		public static implicit operator string(Response Manager)
		{
			return Manager.ToString();
		}
	}
}