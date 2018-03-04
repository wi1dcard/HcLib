using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExample
{
	class Program
	{
		static void Main(string[] args)
		{
			var http = new HcHttp.HttpClient()
			{
				
			};
			http.m_Cookies.Add("cid=PfQdaFqbnE2izWCtBtepAg==");
			var body = new HcHttp.RequestBody.FormData();
			body["smfile"] = new HcHttp.RequestBody.FormData.File(@"C:\Users\abcab\OneDrive\图片\生日.jpg");
			body["file_id"] = "0";
			var res = http.Request(
				"https://sm.ms/api/upload?inajax=1&ssl=1",
				HcHttp.Method.POST,
				body
				);
			Console.Write(res);
			Console.Read();
		}
	}
}
