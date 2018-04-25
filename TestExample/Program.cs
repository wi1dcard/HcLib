using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TestExample
{
	class Program
	{
		static void Main(string[] args)
		{
			// HTTP Client 实例
			var http = new HcHttp.HttpClient();

			// 新增Cookie
			http.m_Cookies.Add("cid=PfQdaFqbnE2izWCtBtepAg==");

			// 请求体实例
			var body = new HcHttp.RequestBody.FormData();

			// 带文件参数
			body["smfile"] = new HcHttp.RequestBody.FormData.File(@"C:\Users\abcab\OneDrive\图片\生日.jpg");

			// 带其他参数
			body["file_id"] = "0";

			// 提交请求
			var res = http.Request(
				"https://sm.ms/api/upload?inajax=1&ssl=1",
				HcHttp.Method.POST,
				body
				);

			// 输出结果
			Console.Write(res);
			Console.Read();
		}
	}
}
