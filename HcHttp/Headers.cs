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
	/// <summary>
	/// 头信息管理类
	/// </summary>
	public class Headers : WebHeaderCollection
	{
		/// <summary>
		/// 空构造函数
		/// </summary>
		public Headers() : base()
		{

		}

		/// <summary>
		/// 根据Cookie对象初始化
		/// </summary>
		/// <param name="Cookies"></param>
		public Headers(Cookies Cookies) : this()
		{
			if(Cookies != null)
			{
				string CookieAsString = Cookies.ToString();
				if (!string.IsNullOrEmpty(CookieAsString))
				{
					this.Set("Cookie", CookieAsString);
				}
			}
		}

		/// <summary>
		/// 根据头信息字符串初始化
		/// </summary>
		/// <param name="Headers"></param>
		public Headers(string Headers)
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

		public bool ContainsKey(string key)
		{
			if (this.Get(key) == null)
			{
				return this.AllKeys.Contains(key);
			}
			return true;
		}

	}
}
