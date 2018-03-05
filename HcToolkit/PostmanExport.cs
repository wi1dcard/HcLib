using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HcToolkit
{
	class PostmanExport
	{
		public class Info
		{
			public string name;
			public string _postman_id;
			public string description;
			public string schema;
		}

		public class Item
		{
			public class Request
			{
				public interface IKeyValue
				{
					string key { get; set; }
					string value { get; set; }
				}
				public class Header : IKeyValue
				{
					public string key { get; set; }
					public string value { get; set; }
					public string description { get; set; }
				}

				public class Body
				{
					public class Item : IKeyValue
					{
						public string key { get; set; }
						public string value { get; set; }
						public string description { get; set; }
						public string type { get; set; }
					}

					public string mode;
					public Item[] urlencoded;
					public Item[] formdata;
				}

				public class Url
				{
					public class Query : IKeyValue
					{
						public string key { get; set; }
						public string value { get; set; }
						public bool equals;
					}

					public string raw;
					public string protocal;
					public string[] host;
					public string[] path;
					public Query[] query;
				}

				public string method;
				public Header[] header;
				public Body body;
				public Url url;
				public string description;
			}

			public string name;
			public Request request;
			public object response;
		}

		public Info info;
		public Item[] item;
	}
}
