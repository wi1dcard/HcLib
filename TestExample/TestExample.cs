using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.IO;

using HcHttp;
using MSScriptControl;

namespace TestExample
{
    public partial class TestExample : Form
    {
        public TestExample()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
			////string str = new Uri("system/123", UriKind.RelativeOrAbsolute).IsAbsoluteUri ? "yes" : "no";
			////string str = new Uri(new Uri("/www/"), "http://www.baidu.com").ToString();
			////Debug.WriteLine(str);
			////return;

			//Http a = new Http();
			//var cb = new Http.Callback()
			//{
			//	OnRecving = (c, x, y) => { Debug.WriteLine("recv: " + c.ToString() + " " + x.ToString() + " " + y.ToString()); },
			//	OnSending = (c, x, y) => { Debug.WriteLine("send: " + c.ToString() + " " + x.ToString() + " " + y.ToString()); }
			//};
			//var body = new Http.Body.Multipart();
			//body["username"] = "admin";
			//body["fuck"] = new Http.Body.Multipart.File(@"C:\Users\Administrator\Desktop\项目.png");
			//body["password"] = "admin";
			//{
			//	Http.Response res = a.Request("http://localhost./index/user/login", Method.POST, body, null, cb);
			//	MessageBox.Show(res.Text);

			//}

			Http http = new Http();
			Http.Body.Form body = new Http.Body.Form();

			body["email"] = "QinXQDcFangU@yahoo.com";
			body["password"] = "Aa112233";
			body["country"] = "CN";
			body["phoneNumber"] = "";
			body["passwordForPhone"] = "";
			body["_rememberMe"] = "on";
			body["rememberMe"] = "on";
			body["_eventId"] = "submit";
			body["gCaptchaResponse"] = "";
			body["isPhoneNumberLogin"] = "false";
			body["isIncompletePhone"] = "";

			var res = http.Request("https://signin.ea.com/p/web2/login?execution=e1659379175s1&initref=https%3A%2F%2Faccounts.ea.com%3A443%2Fconnect%2Fauth%3Fprompt%3Dlogin%26accessToken%3Dnull%26client_id%3DFIFA-18-WEBCLIENT%26response_type%3Dtoken%26display%3Dweb2%252Flogin%26locale%3Den_US%26redirect_uri%3Dhttps%253A%252F%252Fwww.easports.com%252Ffifa%252Fultimate-team%252Fweb-app%252Fauth.html%26scope%3Dbasic.identity%2Boffline%2Bsignin"
				, Method.POST, body);
            
        }
    }
}
