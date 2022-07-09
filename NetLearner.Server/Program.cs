using EasyNetQ;
using NetLearner.DTO.SendModels;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace NetLearner.Server
{
	class Program
	{
		private static Program _program = new Program();

		IBus Hutch { get; set; }
		private static void Log(string message) => Console.WriteLine("[log] " + message);


		static void Main(string[] args)
		{

			//@Берёт каталог с даркнетом, цфг и data. Для конечного приложинея нужно изменить.
			var tmp = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetDirectories().Where(i => i.Name == "NeuralNetwork").ToArray()/*[0]*/;
			if (tmp.Length == 0)
			{
				Log("Не обнаружено папки \"NeuralNetwork\" по пути " + AppDomain.CurrentDomain.BaseDirectory);
			}
			else
			{
				_program.MaidNeuralNetDir = tmp[0];

				_program.DarknetPath = _program.MaidNeuralNetDir.FullName + "\\darknet\\darknet.exe";
				_program.BackupFolder = _program.MaidNeuralNetDir.FullName + "\\backup";
			}


			JObject json = JObject.Parse(File.ReadAllText(@"RabbitConnection.json"));
			string connection = json["ConnectionString"].ToString();
			_program.Hutch = RabbitHutch.CreateBus(connection);
			_program.Hutch.Rpc.Respond<StartFeatures, RabbitRespond>(x =>
			{
				TrainingLauncher.StartTraining(_program.DarknetPath, x.DataFilePath,
				x.CfgFilePath, x.WeightPath, x.IsNeedmAP, x.IsPreClear, x.CountGPUs);

				Log("Start Learn!!!");
				return new RabbitRespond();
			});

			_program.Hutch.Rpc.Respond<mAPFeature, RabbitRespondText>(x =>
			{
				string answer = TrainingLauncher.GetMapInfo(_program.DarknetPath, x.DataFilePath,
				x.CfgFilePath, x.WeightPath);

				Log("mAP was calculated");
				return new RabbitRespondText() { Answer = answer };
			});

			
			_program.Hutch.Rpc.Respond<StopSignal, RabbitRespond>(x =>
			{
				TrainingLauncher.Stop();
				Log("STOP Learn!!!");
				return new RabbitRespond();
			});

			//Тут следим за папкой с весами
			_program.watcher = new FileSystemWatcher(_program.BackupFolder + '\\');
			_program.watcher.EnableRaisingEvents = true;
			_program.watcher.Created += _program.OnCreated;

			Console.ReadLine();
		}
		FileSystemWatcher watcher;
		private void OnCreated(object sender, FileSystemEventArgs e)
		{
			var model = new FilePath
			{
				FullPath = e.FullPath,
				Name = e.Name,
				Time = DateTime.Now
			};
			var answer = _program.Hutch.Rpc.Request<FilePath, RabbitRespond>(model);
			if (answer.IsSuccess)
				Log("Weight file " + e.Name + " was created");
		}

		public DirectoryInfo MaidNeuralNetDir { get; private set; }
		public string DarknetPath { get; set; }
		public string BackupFolder { get; set; }
		public string Connection { get; set; }



	}
}
