using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLearner.Client
{
	/// <summary>
	/// Настройки для <see cref="OpenFileDialog"/>
	/// </summary>
	class FDInfo
	{
		public FDInfo(string title, string filter = "", string initialDirectory = "")
		{
			Title = title;
			Filter = filter;
			InitialDirectory = initialDirectory;
		}
		public string FilePath { get; set; }
		public string InitialDirectory { get; private set; }
		public string Filter { get; private set; }
		public string Title { get; private set; }
	}
}
