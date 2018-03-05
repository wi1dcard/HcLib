using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HcToolkit
{
	class CodeGenerator
	{
		
		public CodeGenerator()
		{
			this.usings = @"";
			this.description = @"本代码使用 HcToolkit 生成。";
			this.classes = new List<Class>();
		}

		public class Class
		{
			public Class()
			{
				this.headers = new List<Header>();
				this.parameters = new List<Parameter>();
			}
			
			public class Parameter
			{
				public string name;
				public string type;
				public string description;
				public string defaultValue;
			}

			public class Header
			{
				public string name;
				public string description;
				public string defaultValue;
			}

			public string name;
			public string description { get; set; }

			public string method;
			public string uri;
			public List<Header> headers;
			public List<Parameter> parameters;
		}

		public string filename;
		public string usings;
		public string description;
		public string baseUri;
		public string @namespace;
		public List<Class> classes;

		public int gen()
		{
			var codeClasses = "";
			foreach(var c in this.classes)
			{
				var codeHeaders = "";
				foreach(var h in c.headers)
				{
					codeHeaders +=
						string.Format(CodeTemplate.header, h.description, h.name, h.defaultValue);
				}
				
				var codeParams = "";
				foreach(var p in c.parameters)
				{
					codeParams +=
						string.Format(CodeTemplate.parameter, p.description, p.type, p.name, p.defaultValue);
				}

				codeClasses +=
					string.Format(CodeTemplate.@class, c.description, c.name, c.method, c.uri, codeHeaders, codeParams);
			}
			var code =
				string.Format(CodeTemplate.@namespace, this.usings, this.description, this.@namespace, codeClasses, this.baseUri)
				.Trim();
			
			System.IO.File.WriteAllText(this.filename, code);
			return code.Length;
		}
	}
}
