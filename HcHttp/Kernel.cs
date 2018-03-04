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

	public class Request
	{
		public Method Method = Method.GET;

		public string BaseUri = "";

		public string Uri = "";

		public Headers Headers;

		public Cookies Cookies;

		public RequestBody.IBody Content;

		public Callbacks Callbacks;

		public bool AutoRedirect = true;

		public int Timeout = int.MaxValue;

		public RequestCacheLevel CacheLevel = RequestCacheLevel.NoCacheNoStore;
	}

	public static class Kernel
	{
		/// <summary>
		/// 发起请求
		/// </summary>
		public static Response Request(Request Rqt)
		{
			//基本设置
			Kernel.SetAllowUnsafeHeaderParsing(true);
			var Uri = Rqt.Uri;
			if (!(new Uri(Uri, UriKind.RelativeOrAbsolute).IsAbsoluteUri))
			{
				Uri = (string.IsNullOrWhiteSpace(Rqt.BaseUri) ? "" : Rqt.BaseUri) + Uri;
			}
			if (Rqt.Method == Method.GET && Rqt.Content != null && Rqt.Content is RequestBody.FormUrlEncoded)
			{
				Uri += "?" + Rqt.Content.ToString();
			}
			HttpWebRequest Request = (HttpWebRequest)HttpWebRequest.Create(Uri);
			var Callbacks = Rqt.Callbacks ?? new Callbacks();

			byte[] RequestBody = { };
			switch (Rqt.Method)
			{
				case Method.GET:
					Request.Method = "GET";
					break;
				case Method.POST:
					Request.Method = "POST";
					if (Rqt.Content == null)
					{
						throw new System.ArgumentException("Content Could Not Be NULL");
					}
					else
					{
						RequestBody				= Rqt.Content.Raw;
						Request.ContentType		= Rqt.Content.ContentType;
						Request.ContentLength	= Rqt.Content.ContentLength;
					}
					break;
				case Method.HEAD:
					Request.Method = "HEAD";
					break;
				default:
					throw new System.ArgumentException("Unknown HTTP Request Method");
			}

			//参数设置
			Request.AllowAutoRedirect = Rqt.AutoRedirect;
			//Request.ReadWriteTimeout = (int)Rqt.Timeout;
			Request.Timeout = (int)Rqt.Timeout;

			//通用设置
			Request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;
			Request.CachePolicy = new RequestCachePolicy(Rqt.CacheLevel);
			Request.KeepAlive = true;
			Request.Accept = "*/*";

			//需要使用属性设置的特殊请求头
			var RemoveHeaders = new string[] { "Referer", "User-Agent", "Accept", "Keep-Alive", "Connection", "Content-Length", "Content-Type", "Host", "If-Modified-Since" };
			foreach (var HeaderName in RemoveHeaders)
			{
				if (Rqt.Headers != null && !string.IsNullOrEmpty(Rqt.Headers.Get(HeaderName)))
				{
					var HeaderNameWithoutHyphens = HeaderName.Replace("-", "");
					var HeaderValue = Rqt.Headers[HeaderName];
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
					Rqt.Headers.Remove(HeaderName);
				}
			}

			//请求头
			var RequestHeaders = Rqt.Headers == null ? (Rqt.Cookies == null ? new Headers() : new Headers(Rqt.Cookies)) : Rqt.Headers;
			if (RequestHeaders != null)
			{
				foreach (string Key in RequestHeaders.AllKeys)
				{
					Request.Headers.Set(Key, RequestHeaders[Key]);
				}
			}

			//发送请求
			Callbacks.OnStatus(Callbacks.Status.SendStart, Request.ContentLength);//开始发送
			#region 发送请求
			if (Rqt.Method == Method.POST)
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
			Callbacks.OnStatus(Callbacks.Status.SendFinish, Request.ContentLength);//发送完毕

			Callbacks.OnStatus(Callbacks.Status.RecvStart, Response.ContentLength);//开始接收
			#region 接收响应
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
			Callbacks.OnStatus(Callbacks.Status.RecvFinish, Stream.Length);//接收完毕

			//分析响应
			ResponseManager = new Response(Response, Stream);
			if (Rqt.Cookies == null)
			{
				Rqt.Cookies = new Cookies();
			}
			Rqt.Cookies.Add(Response.GetResponseHeader("Set-Cookie"), Cookies.CookieType.ResponseHeader);
			Response.Close();
			return ResponseManager;
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
	}
}