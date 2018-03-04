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
		public enum CookieType
		{
			RequestHeader,
			ResponseHeader,
		}

		public Cookies()
		{

		}

		public Cookies(string Cookies, CookieType Type = CookieType.RequestHeader)
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
}