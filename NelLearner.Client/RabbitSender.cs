using EasyNetQ;
using NetLearner.DTO.SendModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLearner.Client
{
	public class RabbitSender
	{
		//The EasyNetQ connection string. Example: host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
		//The following default values will be used if not specified: host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
		public RabbitSender(string connectionString = "host=localhost")
		{
			bus = RabbitHutch.CreateBus(connectionString);
			Connection = connectionString;

			bus.Rpc.Respond<FilePath, RabbitRespond>(x =>
			{
				this?.onChanged(x);
				return new RabbitRespond();
			});
		}
		string _connection;
		public string Connection
		{
			get => _connection;
			set
			{
				bus = RabbitHutch.CreateBus(value);
				_connection = value;
			}
		}

		IBus bus;

		public void SendStart(string dataFilePath, string cfgFilePath, string weightPath, bool graphForm = false, bool isPreClear = false, int CountGPUs = 1)
		{
			//string path = @"D:\Desktop\Project\WinModule2+Rabbit\LearnerСlient\NelLearner.Client\Helpers\151.png";
			var model = new StartFeatures();
			model.CfgFilePath = cfgFilePath;
			model.DataFilePath = dataFilePath;
			model.WeightPath = weightPath;
			model.IsNeedmAP = graphForm;
			model.IsPreClear = isPreClear;
			model.CountGPUs = CountGPUs;

			var answer = bus.Rpc.Request<StartFeatures, RabbitRespond>(model);

			Trace.WriteLine(answer.IsSuccess);
		}

		public Action<FilePath> onChanged;


		public string GetMAP(string dataFilePath, string cfgFilePath, string weightPath) 
		{
			var model = new mAPFeature();
			model.CfgFilePath = cfgFilePath;
			model.DataFilePath = dataFilePath;
			model.WeightPath = weightPath;
			var answer = bus.Rpc.Request<mAPFeature, RabbitRespondText>(model);

			return answer.Answer;
		}
		public void SendStop()
		{
			var answer = bus.Rpc.Request<StopSignal, RabbitRespond>(new StopSignal());
			Trace.WriteLine(answer.IsSuccess);
		}

	}
}
