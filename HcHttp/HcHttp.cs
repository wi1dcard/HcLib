/*********************************************************************************
 * Copyright(C) 2017
 * FileName:	HcHttp.cs
 * Author:		红船
 * Version:		2.2
 * Date:		2017-10-19
 * License:		MIT
 * Website:		https://rblog.cc/
 * 良好的开源环境需要大家共同维护，请自重！
**********************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.IO;
using System.Text.RegularExpressions;

namespace HcHttp
{
	public enum Method
	{
		GET,
		POST,
		HEAD
	}

	public class Http
	{
		/// <summary>
		/// Cookies对象
		/// </summary>
		public Cookie m_Cookies { get; set; }
		
		/// <summary>
		/// User-Agent头
		/// </summary>
		public string m_UserAgent { get; set; }

		/// <summary>
		/// 设定BaseUrl
		/// </summary>
		public string m_BaseUrl { get; set; }

		/// <summary>
		/// 设定Cache-Level
		/// </summary>
		public RequestCacheLevel m_CacheLevel { get; set; }

		/// <summary>
		/// 设定错误回调
		/// </summary>
		public Func<Exception, Response> m_ErrorHandler { get; set; }

		public Http()
		{
			m_Cookies = new Cookie();
			m_UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.116 Safari/537.36 TheWorld 7";
			m_BaseUrl = "";
			m_CacheLevel = RequestCacheLevel.NoCacheNoStore;
			Http.SetAllowUnsafeHeaderParsing(true);
		}

		/// <summary>
		/// 发起请求
		/// </summary>
		/// <param name="Uri">请求Uri；当BaseUrl被设定时，请求Url为：BaseUrl + Uri</param>
		/// <param name="Method">请求方式；</param>
		/// <param name="Content">POST请求体</param>
		/// <param name="Headers">请求头信息</param>
		/// <param name="Callbacks">回调；通常用于下载</param>
		/// <param name="AutoRedirect">是否跟随302跳转</param>
		/// <param name="Timeout">请求超时时间</param>
		/// <returns></returns>
		public Response Request(string Uri,
								 Method Method = Method.GET,
								 IBody Content = null,
								 Header Headers = null,
								 Callback Callbacks = null,
			//参数设置
								 bool AutoRedirect = true,
								 uint Timeout = int.MaxValue
							   )
		{
			try
			{
				//基本设置
				if (!(new Uri(Uri, UriKind.RelativeOrAbsolute).IsAbsoluteUri))
				{
					Uri = m_BaseUrl + Uri;
				}
				if (Method == Method.GET && Content != null && Content is Body.Form)
				{
					Uri += "?" + Content.ToString();
				}
				HttpWebRequest Request = (HttpWebRequest)HttpWebRequest.Create(Uri);
				Callbacks = Callbacks ?? new Callback();

				byte[] RequestBody = { };
				switch (Method)
				{
					case Method.GET:
						Request.Method = "GET";
						break;
					case Method.POST:
						Request.Method = "POST";
						if (Content == null)
						{
							Request.ContentLength = 0;
						}
						else
						{
							RequestBody = Content.Binary;
							Request.ContentType = Content.Type;
							Request.ContentLength = RequestBody.Length;
						}
						break;
					case Method.HEAD:
						Request.Method = "HEAD";
						break;
					default:
						throw new Exception("错误的请求方法");
				}

				//参数设置
				Request.AllowAutoRedirect = AutoRedirect;
				Request.ReadWriteTimeout = (int)Timeout;
				Request.UserAgent = m_UserAgent;

				//通用设置
				Request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;
				Request.CachePolicy = new RequestCachePolicy(m_CacheLevel);
				Request.KeepAlive = true;
				Request.Accept = "*/*";

				//特殊请求头
				var RemoveHeaders = new string[] { "Referer", "UserAgent", "Accept", "KeepAlive" };
				foreach (var HeaderName in RemoveHeaders)
				{
					if (Headers != null && !string.IsNullOrEmpty(Headers.Get(HeaderName)))
					{
						Request.GetType().GetProperty(HeaderName).SetValue(Request, Headers[HeaderName], null);
						Headers.Remove(HeaderName);
					}
				}

				//请求头
				var RequestHeaders = Headers == null ? new Header(m_Cookies) : Headers;
				if (RequestHeaders != null)
				{
					foreach (string Key in RequestHeaders.AllKeys)
					{
						Request.Headers.Set(Key, RequestHeaders[Key]);
					}
				}

				//发送请求
				Callbacks.OnStatus(Callback.Status.SendStart, Request.ContentLength);//开始发送
				#region 发送请求
				if (Method == Method.POST)
				{
					//提交请求
					using (var RequestStream = Request.GetRequestStream())
					{
						if (Callbacks.OnSending == null)
						{
							RequestStream.Write(RequestBody, 0, RequestBody.Length);
						}
						else
						{
							int totalBytes = RequestBody.Length;
							int totalUploadedBytes = 0;
							int uploadedBytes = 4096;
							for (int offset = 0; offset < totalBytes; offset = totalUploadedBytes)
							{
								totalUploadedBytes += uploadedBytes;
								if (totalUploadedBytes > totalBytes)
								{
									uploadedBytes = totalBytes - offset;
									totalUploadedBytes = totalBytes;
								}
								RequestStream.Write(RequestBody, offset, uploadedBytes);
								Callbacks.OnSending(uploadedBytes, totalUploadedBytes, totalBytes);
							}
						}
					}
				}

				HttpWebResponse Response = null;
				try
				{
					//接收响应
					Response = (HttpWebResponse)Request.GetResponse();
				}
				catch (WebException e)
				{
					Response = (HttpWebResponse)e.Response;
					if (Response == null)
					{
						throw;
					}
				}
				#endregion
				Callbacks.OnStatus(Callback.Status.SendFinish, Request.ContentLength);//发送完毕

				Callbacks.OnStatus(Callback.Status.RecvStart, Response.ContentLength);//开始接收
				#region 接收请求
				Response ResponseManager = null;
				MemoryStream Stream = new MemoryStream();
				using (Stream ResponseStream = Response.GetResponseStream())
				{
					if (Callbacks.OnRecving == null)
					{
						ResponseStream.CopyTo(Stream);
					}
					else
					{
						long totalBytes = Response.ContentLength;
						long totalDownloadedBytes = 0;
						int downloadedBytes = 0;
						byte[] buff = new byte[4096];
						do
						{
							downloadedBytes = ResponseStream.Read(buff, 0, (int)buff.Length);
							if (downloadedBytes == 0)
							{
								break;
							}
							totalDownloadedBytes += downloadedBytes;
							Stream.Write(buff, 0, downloadedBytes);
							Callbacks.OnRecving(downloadedBytes, totalDownloadedBytes, totalBytes);
						} while (true);
					}
				}
				#endregion
				Callbacks.OnStatus(Callback.Status.RecvFinish, Stream.Length);//接收完毕

				//分析请求
				ResponseManager = new Response(Response, Stream);
				m_Cookies.Add(Response.GetResponseHeader("Set-Cookie"), Cookie.CookieType.ResponseHeader);
				Response.Close();
				return ResponseManager;
			}
			catch (Exception ex)
			{
				if (m_ErrorHandler == null)
				{
					throw;
				}
				else
				{
					return m_ErrorHandler(ex);
				}
			}
		}

		private static bool SetAllowUnsafeHeaderParsing(bool useUnsafe)
		{
			//Get the assembly that contains the internal class
			System.Reflection.Assembly aNetAssembly = System.Reflection.Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
			if (aNetAssembly != null)
			{
				//Use the assembly in order to get the internal type for the internal class
				Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
				if (aSettingsType != null)
				{
					//Use the internal static property to get an instance of the internal settings class.
					//If the static instance isn't created allready the property will create it for us.
					object anInstance = aSettingsType.InvokeMember("Section",
					  System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.NonPublic, null, null, new object[] { });

					if (anInstance != null)
					{
						//Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
						System.Reflection.FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
						if (aUseUnsafeHeaderParsing != null)
						{
							aUseUnsafeHeaderParsing.SetValue(anInstance, useUnsafe);
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Cookie管理类
		/// </summary>
		public class Cookie : CookieCollection
		{
			public enum CookieType
			{
				RequestHeader,
				ResponseHeader,
			}

			public Cookie()
			{

			}

			public Cookie(string Cookies, CookieType Type = CookieType.RequestHeader)
			{
				this.Add(Cookies, Type);
			}

			public void Add(string Cookies, CookieType Type = CookieType.RequestHeader)
			{
				Regex regex = null;
				switch (Type)
				{
					case CookieType.RequestHeader:
						regex = new Regex(@"(\w+?)=(.+?)(?:;|$)");
						break;
					case CookieType.ResponseHeader:
						regex = new Regex(@"(?:^|,)([\w\.]+?)=(.+?)(?:;|$)");
						break;
				}
				MatchCollection matches = regex.Matches(Cookies);
				foreach (Match match in matches)
				{
					string cookie_name = match.Groups[1].ToString();
					string cookie_value = match.Groups[2].ToString();
					this[cookie_name] = cookie_value;
				}
			}

			public string this[string Cookie]
			{
				get
				{
					return base[Cookie].Value;
				}
				set
				{
					base.Add(new System.Net.Cookie(Cookie, value));
				}
			}

			public override string ToString()
			{
				StringBuilder res = new StringBuilder(this.Count * 128);
				foreach (System.Net.Cookie cookie in this)
				{
					res.Append("; ");
					res.Append(cookie.ToString());
				}
				if (res.Length > 0)
				{
					res.Remove(0, 2);
				}
				return res.ToString();
			}
		}

		/// <summary>
		/// Content管理类
		/// </summary>
		public class Body : IBody
		{
			public Body() { }

			public string Type { get; set; }

			public byte[] Binary { get; set; }

			public string Content
			{
				get
				{
					return Encoding.UTF8.GetString(this.Binary);
				}

				set
				{
					Binary = Encoding.UTF8.GetBytes(value);
				}
			}

			public override string ToString()
			{
				return this.Content;
			}

			//======================================================================

			public class Form : Dictionary<string, string>, IBody
			{
				public Form() { }

				public string Type
				{
					get
					{
						return "application/x-www-form-urlencoded";
					}
				}

				public byte[] Binary
				{
					get
					{
						return Encoding.UTF8.GetBytes(this.Content);
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

			public class Multipart : Dictionary<string, object>, IBody
			{
				private string Boundary;

				public Multipart()
				{
					Boundary = Guid.NewGuid().ToString("N");
				}

				public string Type
				{
					get
					{
						return "multipart/form-data; boundary=" + this.Boundary;
					}
				}

				public byte[] Binary
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
								if (File.Content != null)
								{
									StreamWriter.Write("Content-Disposition: form-data; name=\"" + Kv.Key + "\"; filename=\"" + File.Filename + "\"\r\n");
									StreamWriter.Write("Content-Type: application/octet-stream\r\n");
									StreamWriter.Write("\r\n");
									Stream.Write(File.Content, 0, File.Content.Length);
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
					public byte[] Content { get; set; }

					public string Filename { get; set; }

					public File() { }

					public File(string Filename)
					{
						if (System.IO.File.Exists(Filename))
						{
							this.Content = System.IO.File.ReadAllBytes(Filename);
							FileInfo FI = new FileInfo(Filename);
							this.Filename = FI.Name;
						}
					}
				}
			}

		}

		public interface IBody
		{
			string Type { get; }
			byte[] Binary { get; }
		}

		/// <summary>
		/// 回调管理类
		/// </summary>
		public class Callback
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

			public Callback()
			{
				this.OnStatus = (x, y) =>
				{
					//System.Diagnostics.Debug.WriteLine(x);
				};
			}
		}

		/// <summary>
		/// 头信息管理类
		/// </summary>
		public class Header : WebHeaderCollection
		{
			public Header()
			{

			}

			public Header(Cookie Cookies)
				: this()
			{
				string Cookie = Cookies.ToString();
				if (!string.IsNullOrEmpty(Cookie))
				{
					this.Set("Cookie", Cookie);
				}
			}

			public Header(string Headers)
				: this()
			{
				Headers = Headers.Replace("\r", "");
				Regex regex = new Regex(@"^([^:\s]+)\s*:\s*(.+)$", RegexOptions.Multiline);
				MatchCollection matches = regex.Matches(Headers);
				foreach (Match match in matches)
				{
					string header_name = match.Groups[1].ToString();
					string header_value = match.Groups[2].ToString();
					this.Set(header_name, header_value);
				}
			}

		}

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
}

