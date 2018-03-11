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
	public class HttpClient
	{
		/// <summary>
		/// Cookies对象
		/// </summary>
		public Cookies m_Cookies { get; set; }

		/// <summary>
		/// User-Agent头
		/// </summary>
		public string m_UserAgent { get; set; }

		/// <summary>
		/// 设定BaseUrl
		/// </summary>
		public string m_BaseUri { get; set; }

		/// <summary>
		/// 设定Cache-Level
		/// </summary>
		public RequestCacheLevel m_CacheLevel { get; set; }

		/// <summary>
		/// 设定错误回调
		/// </summary>
		public Func<Exception, Response> m_ErrorHandler { get; set; }

		public HttpClient()
		{
			m_BaseUri = "";
			m_Cookies = new Cookies();
			m_UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36";
			m_CacheLevel = RequestCacheLevel.NoCacheNoStore;
		}

		/// <summary>
		/// 发起请求
		/// </summary>
		/// <param name="Uri">请求Uri；当m_BaseUri被设定时，请求Url为：m_BaseUri + Uri</param>
		/// <param name="Method">请求方式</param>
		/// <param name="Content">POST请求体</param>
		/// <param name="Headers">请求头信息</param>
		/// <param name="AutoRedirect">是否跟随302/301跳转</param>
		/// <param name="Timeout">请求超时时间（单位：毫秒）</param>
		/// <param name="Callbacks">回调函数；通常用于上传下载</param>
		/// <returns></returns>
		public Response Request(string Uri,
								Method Method = Method.GET,
								RequestBody.IBody Content = null,
								Headers Headers = null,
								bool AutoRedirect = true,
								int Timeout = 30 * 1000
								)
		{
			try
			{
				if(Headers == null)
				{
					Headers = new Headers();
				}
				Headers.Add("User-Agent", m_UserAgent);
				var Rqt = new Request()
				{
					Method = Method,
					BaseUri = m_BaseUri,
					Uri = Uri,
					Headers = Headers,
					Content = Content,
					AutoRedirect = AutoRedirect,
					Timeout = Timeout,
					Cookies = m_Cookies,
					CacheLevel = m_CacheLevel
				};
				return Rqt.Send();
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

	}
}

