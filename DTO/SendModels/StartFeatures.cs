using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLearner.DTO.SendModels
{
	public class StartFeatures
	{
		public string DataFilePath { get; set; }
		public string CfgFilePath { get; set; }
		public string WeightPath { get; set; }
		public bool IsNeedmAP { get; set; } = false;
		public bool IsPreClear { get; set; } = false;
		public int CountGPUs { get; set; } = 1;
	}
}
