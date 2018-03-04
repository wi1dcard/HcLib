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
		private byte[] m_Data;
		public byte[] Data
		{
			get
			{
				return m_Data;
			}
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

		private WebHeaderCollection m_Header;
		public WebHeaderCollection Header
		{
			get
			{
				return m_Header;
			}
		}

		private HttpStatusCode m_StatusCode;
		public HttpStatusCode StatusCode
		{
			get
			{
				return m_StatusCode;
			}
		}

		private string m_StatusDescription;
		public string StatusDescription
		{
			get
			{
				return m_StatusDescription;
			}
		}

		private string m_Method;
		public string Method
		{
			get
			{
				return m_Method;
			}
		}

		private Uri m_ResponseUri;
		public Uri ResponseUri
		{
			get
			{
				return m_ResponseUri;
			}
		}

		private string m_Charset;
		public string Charset
		{
			get
			{
				return m_Charset;
			}
		}

		public Response(HttpWebResponse Response, MemoryStream Stream)
		{
			this.m_Header = Response.Headers;
			this.m_StatusCode = Response.StatusCode;
			this.m_StatusDescription = Response.StatusDescription;
			this.m_Method = Response.Method;
			this.m_ResponseUri = Response.ResponseUri;
			this.m_Data = Stream.ToArray();
			this.m_Charset = Response.CharacterSet;
		}

		private string GetText()
		{
			Encoding encoding = Encoding.UTF8;
			try
			{
				encoding = Encoding.GetEncoding(this.m_Charset);
			}
			catch
			{
				var meta = Regex.Match(Encoding.Default.GetString(this.m_Data), "<meta[^<]*charset=\"?(.*?)\"", RegexOptions.IgnoreCase);
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
			return encoding.GetString(this.m_Data);
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