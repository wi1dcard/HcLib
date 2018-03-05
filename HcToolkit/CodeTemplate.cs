namespace HcToolkit
{
	static class CodeTemplate
	{
		public const string baseClass = "APIBase";

		public static string @namespace = @"
{0}
/// <summary>
/// {1}
/// </summary>
namespace {2}
{{
	class " + baseClass + @" : " + typeof(HcHttp.APIRequest).FullName + @"
	{{
		public " + baseClass + @"()
		{{
			base.BaseUri = " + "@\"{4}\"" + @";
		}}
	}}
{3}
}}";

		public static string @class = @"
	/// <summary>
	/// {0}
	/// </summary>
	class {1} : " + baseClass + @"
	{{
		public {1}()
		{{
			base.Method = " + typeof(HcHttp.Method).FullName + @".{2};
			base.Uri = " + "@\"{3}\"" + @";
			{4}
		}}
	{5}
	}}
";

		public static string @parameter = @"
		/// <summary>{0}</summary>
		/// <example>{3}</example>
		public {1} _{2} {{ get; set; }}
";

		public static string @header = @"
			// {0}
			base.Headers.Add(" + "\"{1}\"" + ", " + "\"{2}\"" + @");
";
	}
}