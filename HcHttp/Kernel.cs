using System;
using System.IO;
using System.Net;
using System.Net.Cache;

namespace HcHttp
{
	public enum Method
	{
		GET,
		POST,
		HEAD
	}

	public class Request
	{
		public Request()
		{
			this.Method = Method.GET;
			this.BaseUri = "";
			this.Uri = "";
			this.AutoRedirect = true;
			this.Timeout = int.MaxValue;
			this.CacheLevel = RequestCacheLevel.NoCacheNoStore;
			this.TransmitChunkBytes = 4096;
			this.SetAllowUnsafeHeaderParsing(true);
		}

		public Method Method { get; set; }

		public string BaseUri { get; set; }

		public string Uri { get; set; }

		public Headers Headers { get; set; }

		public Cookies Cookies { get; set; }

		public RequestBody.IBody Content { get; set; }

		public bool AutoRedirect { get; set; }

		public int Timeout { get; set; }

		public RequestCacheLevel CacheLevel { get; set; }

		public int TransmitChunkBytes { get; set; }

		private bool SetAllowUnsafeHeaderParsing(bool useUnsafe)
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

		public event EventHandler<TransmitEventArgs> OnSending;

		public event EventHandler<TransmitEventArgs> OnRecving;

		public event EventHandler<StatusEventArgs> OnStatus;

		public virtual Response Send()
		{
			//基本设置
			var Uri = this.Uri;
			if (!(new Uri(Uri, UriKind.RelativeOrAbsolute).IsAbsoluteUri))
			{
				Uri = (string.IsNullOrWhiteSpace(this.BaseUri) ? "" : this.BaseUri) + Uri;
			}
			if (this.Method != Method.POST && this.Content != null && this.Content is RequestBody.FormUrlEncoded)
			{
				Uri += "?" + this.Content.ToString();
			}
			HttpWebRequest Request = (HttpWebRequest)HttpWebRequest.Create(Uri);

			byte[] RequestBody = { };
			switch (this.Method)
			{
				case Method.GET:
					Request.Method = "GET";
					break;
				case Method.POST:
					Request.Method = "POST";
					if (this.Content == null)
					{
						throw new System.ArgumentException("Content Could Not Be NULL");
					}
					else
					{
						RequestBody = this.Content.Raw;
						Request.ContentType = this.Content.ContentType;
						Request.ContentLength = this.Content.ContentLength;
					}
					break;
				case Method.HEAD:
					Request.Method = "HEAD";
					break;
				default:
					throw new System.ArgumentException("Unknown HTTP Request Method");
			}

			//参数设置
			Request.AllowAutoRedirect = this.AutoRedirect;
			//Request.ReadWriteTimeout = (int)this.Timeout;
			Request.Timeout = (int)this.Timeout;

			//通用设置
			Request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;
			Request.CachePolicy = new RequestCachePolicy(this.CacheLevel);
			Request.KeepAlive = true;
			Request.Accept = "*/*";

			//需要使用属性设置的特殊请求头
			var RemoveHeaders = new string[] { "Referer", "User-Agent", "Accept", "Keep-Alive", "Connection", "Content-Length", "Content-Type", "Host", "If-Modified-Since" };
			foreach (var HeaderName in RemoveHeaders)
			{
				if (this.Headers != null && !string.IsNullOrEmpty(this.Headers.Get(HeaderName)))
				{
					var HeaderNameWithoutHyphens = HeaderName.Replace("-", "");
					var HeaderValue = this.Headers[HeaderName];
					var TargetType = Request.GetType().GetProperty(HeaderNameWithoutHyphens).PropertyType;
					object HeaderValueAsObject = null;
					switch (TargetType.Name)
					{
						case "Int64":
							HeaderValueAsObject = Int64.Parse(HeaderValue);
							break;
						case "DateTime":
							HeaderValueAsObject = DateTime.Parse(HeaderValue);
							break;
						case "String":
						default:
							HeaderValueAsObject = HeaderValue;
							break;

					}
					Request.GetType().GetProperty(HeaderNameWithoutHyphens).SetValue(Request, HeaderValueAsObject, null);
					this.Headers.Remove(HeaderName);
				}
			}

			//请求头
			var RequestHeaders = this.Headers == null ? (this.Cookies == null ? new Headers() : new Headers(this.Cookies)) : this.Headers;
			if (RequestHeaders != null)
			{
				foreach (string Key in RequestHeaders.AllKeys)
				{
					Request.Headers.Set(Key, RequestHeaders[Key]);
				}
			}

			//发送请求
			this.OnStatus(this, new StatusEventArgs(StatusEventArgs.Status.SendStart, Request.ContentLength));//开始发送
			#region 发送请求
			if (this.Method == Method.POST)
			{
				//提交请求
				using (var RequestStream = Request.GetRequestStream())
				{
					var Handler = this.OnSending.GetInvocationList();
					if (Handler == null || Handler.Length == 0)
					{
						RequestStream.Write(RequestBody, 0, RequestBody.Length);
					}
					else
					{
						int totalBytes = RequestBody.Length;
						int totalUploadedBytes = 0;
						int uploadedBytes = TransmitChunkBytes;
						for (int offset = 0; offset < totalBytes; offset = totalUploadedBytes)
						{
							totalUploadedBytes += uploadedBytes;
							if (totalUploadedBytes > totalBytes)
							{
								uploadedBytes = totalBytes - offset;
								totalUploadedBytes = totalBytes;
							}
							RequestStream.Write(RequestBody, offset, uploadedBytes);
							this.OnSending(this, new TransmitEventArgs(uploadedBytes, totalUploadedBytes, totalBytes));
						}
					}
				}
			}

			HttpWebResponse Response = null;
			try
			{
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
			this.OnStatus(this, new StatusEventArgs(StatusEventArgs.Status.SendFinish, Request.ContentLength));//发送完毕

			this.OnStatus(this, new StatusEventArgs(StatusEventArgs.Status.RecvStart, Response.ContentLength));//开始接收
			#region 接收响应
			Response ResponseManager = null;
			MemoryStream Stream = new MemoryStream();
			using (Stream ResponseStream = Response.GetResponseStream())
			{
				var Handler = this.OnRecving.GetInvocationList();
				if (Handler == null || Handler.Length == 0)
				{
					ResponseStream.CopyTo(Stream);
				}
				else
				{
					long totalBytes = Response.ContentLength;
					long totalDownloadedBytes = 0;
					int downloadedBytes = 0;
					byte[] buff = new byte[TransmitChunkBytes];
					do
					{
						downloadedBytes = ResponseStream.Read(buff, 0, (int)buff.Length);
						if (downloadedBytes == 0)
						{
							break;
						}
						totalDownloadedBytes += downloadedBytes;
						Stream.Write(buff, 0, downloadedBytes);
						this.OnRecving(this, new TransmitEventArgs(downloadedBytes, totalDownloadedBytes, totalBytes));
					} while (true);
				}
			}
			#endregion
			this.OnStatus(this, new StatusEventArgs(StatusEventArgs.Status.RecvFinish, Stream.Length));//接收完毕

			//分析响应
			ResponseManager = new Response(Response, Stream);
			if (this.Cookies == null)
			{
				this.Cookies = new Cookies();
			}
			this.Cookies.Add(Response.GetResponseHeader("Set-Cookie"), Cookies.CookieType.ResponseHeader);
			Response.Close();
			return ResponseManager;
		}
	}

	public class APIRequest : Request
	{
		protected RequestBody.FormData SerializeParamters()
		{
			var body = new RequestBody.FormData();
			var props = this.GetType().GetProperties();
			foreach(var p in props)
			{
				if(p.DeclaringType == typeof(HcHttp.Request))
				{
					continue;
				}
				if (!p.Name.StartsWith("_"))
				{
					continue;
				}
				var k = p.Name.TrimStart('_');
				var v = p.GetValue(this, null);
				body[k] = v;
			}
			return body;
		}

		public override Response Send()
		{
			base.Content = this.SerializeParamters();
			return base.Send();
		}

	}
}