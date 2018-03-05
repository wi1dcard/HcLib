using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace HcToolkit
{
	class Program
	{
		static void Main(string[] args)
		{
			//
			// read parameters
			// 
			string fileInput, fileOutput;
			Uri baseUri;
			try
			{
				fileInput = args[0];
				fileOutput = args.Length > 1 ? args[1] : "";
				baseUri = args.Length > 2 ? new Uri(args[2]) : null;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Invaild parameter: {0}", ex.Message);
				return;
			}

			//
			// read input file
			//
			string jsonString;
			try
			{
				jsonString = File.ReadAllText(fileInput);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("[Error] Could not open file: {0}\n{1}", fileInput, ex.Message);
				return;
			}

			//
			// parse json string
			//
			PostmanExport json = null;
			try
			{
				json = JsonConvert.DeserializeObject<PostmanExport>(jsonString);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("[Error] Json parse error: {0}", ex.Message);
				return;
			}

			// code generator
			var codeGenerater = new CodeGenerator();

			//
			// resolve output dir or file
			//
			if (string.IsNullOrWhiteSpace(fileOutput))
			{
				fileOutput = json.info.name.EscapeIdentifiers() + ".cs";
			}
			else
			{
				var fileOutputInfo = new FileInfo(fileOutput);
				string fileOutputDir;
				if ((fileOutputInfo.Attributes & FileAttributes.Directory) != 0 && !fileOutput.EndsWith(".cs"))
				{
					fileOutputDir = fileOutput;
					fileOutput = Path.Combine(fileOutput, json.info.name.EscapeIdentifiers() + ".cs");
				}
				else
				{
					fileOutputDir = fileOutputInfo.Directory.FullName;
					Directory.CreateDirectory(fileOutputDir);
				}
				Directory.CreateDirectory(fileOutputDir);
			}
			codeGenerater.filename = fileOutput;

			//
			// set basic props
			//
			codeGenerater.@namespace = Path.GetFileNameWithoutExtension(codeGenerater.filename).EscapeIdentifiers();
			codeGenerater.description += json.info.description;
			codeGenerater.baseUri = baseUri == null ? "" : Uri.EscapeUriString(baseUri.OriginalString);

			//
			// transfer classes
			//
			foreach (var item in json.item)
			{
				var @class = new CodeGenerator.Class();

				//
				// resolve uri
				//
				string uri;
				var rawUri = item.request.url.raw;
				var absUri = new Uri(rawUri);
				var absUriWithoutQuery = new Uri(absUri.GetLeftPart(UriPartial.Path));

				var relUri = baseUri == null ? absUri : baseUri.MakeRelativeUri(absUri);
				var relUriWithoutQuery = baseUri.MakeRelativeUri(absUriWithoutQuery);

				@class.name = item.name == rawUri ? item.request.url.path.Last() : item.name;
				if (string.IsNullOrWhiteSpace(@class.name))
					@class.name = relUriWithoutQuery.OriginalString;

				//
				// set basic props
				//
				@class.name = @class.name.EscapeIdentifiers();
				@class.description = item.request.description;
				@class.method = item.request.method;

				//
				// resolve request headers
				//
				foreach (var h in item.request.header)
				{
					@class.headers.Add(new CodeGenerator.Class.Header
					{
						name = h.key,
						description = h.description,
						defaultValue = h.value
					});
				}

				//
				// resolve content-type
				//
				PostmanExport.Item.Request.IKeyValue[] ikv = null;
				if(@class.method == "POST")
				{
					switch (item.request.body.mode)
					{
						case "formdata":
							ikv = item.request.body.formdata;
							break;
						case "urlencoded":
							ikv = item.request.body.urlencoded;
							break;
						case null:
							break;
						default:
							Console.Error.WriteLine("[Warning] Unknown Content-Type: {0} in {1}", item.request.body.mode, item.name);
							break;
					}
					uri = relUri.OriginalString;
				}
				else
				{
					ikv = item.request.url.query;
					uri = relUriWithoutQuery.OriginalString;
				}
				if (ikv == null)
					ikv = new PostmanExport.Item.Request.IKeyValue[0];

				//
				// resolve request params
				//
				foreach (var p in ikv)
				{
					var descProp = p.GetType().GetProperty("description");
					var typeProp = p.GetType().GetProperty("type");
					var type = typeProp == null ? "" : typeProp.GetValue(p).ToString();
					switch (type)
					{
						case "file":
							type = typeof(HcHttp.RequestBody.FormData.File).ReflectedType.FullName;
							break;
						case "":
						case "text":
							type = "string";
							break;
						default:
							Console.Error.WriteLine("[Error] Unknown Parameter Type: {0}", type);
							return;
					}
					@class.parameters.Add(new CodeGenerator.Class.Parameter
					{
						name = p.key.EscapeIdentifiers(),
						defaultValue = p.value,
						type = type,
						description = descProp == null ? "" : string.Format("{0}", descProp.GetValue(p))
					});
				};

				//
				// add class to list
				//
				@class.uri = Uri.EscapeUriString(uri);
				codeGenerater.classes.Add(@class);
			}

			//
			// generate csharp code
			//
			codeGenerater.gen();
		}
	}

	static class Utils
	{
		public static string EscapeIdentifiers(this string s)
		{
			var regex = new Regex(@"[^\w]");
			s = regex.Replace(s, "_");
			return s;
		}
	}

}