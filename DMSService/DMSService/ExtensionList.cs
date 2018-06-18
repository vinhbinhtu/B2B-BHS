using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml;

namespace DMSService
{
	public class ExtensionList
	{
		public class Field
		{
			public string fieldname { get; set; }
			public string oldvalue { get; set; }
			public string type { get; set; }
			public string value { get; set; }
			public string entity { get; set; }
		}

		public class Master
		{
			public string entity { get; set; }
			public List<Field> fields { get; set; }
		}

		public class Detail
		{
			public string entity { get; set; }
			public List<List<Field>> lines { get; set; }
		}

		public class RootObject
		{
			public string type { get; set; }
			public Master master { get; set; }
			public List<Detail> details { get; set; }
		}
	}
}