using System.Net;
using System.Text;
using System.Text.RegularExpressions;
namespace HcHttp
{
	/// <summary>
	/// Cookie管理类
	/// </summary>
	public class Cookies : CookieCollection
	{
		/// <summary>
		/// Cookie类型
		/// </summary>
		public enum CookieType
		{
			/// <summary>
			/// 来自请求头的Cookies字符串
			/// </summary>
			RequestHeader,
			/// <summary>
			/// 来自响应头的Cookies字符串
			/// </summary>
			ResponseHeader,
		}

		/// <summary>
		/// 空构造函数
		/// </summary>
		public Cookies()
		{

		}

		/// <summary>
		/// 根据Cookie(s)字符串初始化
		/// </summary>
		/// <param name="Cookies"></param>
		/// <param name="Type"></param>
		public Cookies(string Cookies, CookieType Type = CookieType.RequestHeader)
		{
			this.Add(Cookies, Type);
		}
		
		/// <summary>
		/// 添加Cookie(s)
		/// </summary>
		/// <param name="Cookies"></param>
		/// <param name="Type"></param>
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

		/// <summary>
		/// 获取、设置指定Cookie
		/// </summary>
		/// <param name="Cookie"></param>
		/// <returns></returns>
		public new string this[string Cookie]
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

		/// <summary>
		/// 将本对象转换成Cookie(s)字符串
		/// </summary>
		/// <returns></returns>
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
}