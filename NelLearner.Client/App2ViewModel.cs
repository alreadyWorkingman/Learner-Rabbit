using EasyNetQ;
using Microsoft.Win32;
using NetLearner.Client.Helpers;
using NetLearner.DTO.SendModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace NetLearner.Client
{

	class App2ViewModel : BindableBase
	{
		public App2ViewModel()
		{
			ListGPUs = new ObservableCollection<string>();
			CountListGPUs = new ObservableCollection<int>() { 1, 2, 3, 4 };
			SelectedCountGPUs = 1;

			LoadGPU.Execute(new object());

			//@Берёт каталог с даркнетом, цфг и data. Для конечного приложинея нужно изменить.
			var tmp = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetDirectories().Where(i => i.Name == "NeuralNetwork").ToArray()/*[0]*/;
			if (tmp.Length == 0)
			{
				MessageBox.Show("Не обнаружено папки \"NeuralNetwork\" по пути " + AppDomain.CurrentDomain.BaseDirectory, "Опаньки");
				EverithingBad = true; //@ КРАЙНЯЯ МЕРА
			}
			else
			{
				maidNeuralNetDir = tmp[0];

				OFDInfoSelectDataset = new FDInfo("Выбрать файл с данными о датасете", "Data files(*.data)|*.data| All files(*.*) | *.*", maidNeuralNetDir.FullName + "\\data");
				OFDInfoSelectCFG = new FDInfo("Выбрать файл конфигурации", "cfg files(*.cfg)|*.cfg| All files(*.*) | *.*", maidNeuralNetDir.FullName + "\\cfg");
				OFDInfoSelectDataWeight = new FDInfo("Выбрать веса для обучения", "");
				SFDInfoCFG = new FDInfo("Выбрать путь сохранения файла конфигураций", "cfg files(*.cfg)|*.cfg", maidNeuralNetDir.FullName + "\\cfg");

				TemplateNormalCFGPath = maidNeuralNetDir.FullName + "\\darknet\\template_yolov3.cfg";
				TemplateTinyCFGPath = maidNeuralNetDir.FullName + "\\darknet\\template_yolov3-tiny.cfg";
			}

			JObject json = JObject.Parse(File.ReadAllText(@"RabbitConnection.json"));
			string connection = json["ConnectionString"].ToString();
			RabbitSender = new RabbitSender(connection);
			RabbitConnection = connection;

			RabbitSender.onChanged = WriteLogFile;
		}
		public DirectoryInfo maidNeuralNetDir { get; private set; }

		//@Крайняя мера
		public bool EverithingBad { get; private set; }

		CfgMaker cfgMaker;
		//Шаблон для создания новых cfg файлов
		string TemplateNormalCFGPath { get; set; }
		string TemplateTinyCFGPath { get; set; }

		private int _WHLearn = 0;
		public int WHLearn
		{
			get => _WHLearn;
			set
			{
				if (OFDInfoSelectCFG.FilePath != null && OFDInfoSelectCFG.FilePath != "")
				{
					if (value > 0 && value % 32 == 0)
						SetProperty<int>(ref _WHLearn, value, "WHLearn");
					CfgMaker.SetCfgValue(OFDInfoSelectCFG.FilePath, "size", _WHLearn.ToString());
				}
			}
		}

		int _subdivisions = 0;
		public int Subdivisions
		{
			get => _subdivisions;
			set
			{
				if (OFDInfoSelectCFG.FilePath != null && OFDInfoSelectCFG.FilePath != "")
				{
					if (value > 0)
						SetProperty<int>(ref _subdivisions, value, "Subdivisions");
					CfgMaker.SetCfgValue(OFDInfoSelectCFG.FilePath, "subdivisions", _subdivisions.ToString());

				}
			}
		}

		bool _isNeddmAP;
		public bool IsNeedmAP
		{
			get => _isNeddmAP;
			set => SetProperty(ref _isNeddmAP, value, "IsNeedmAP");
		}

		bool _isPreClear;
		public bool IsPreClear
		{
			get => _isPreClear;
			set => SetProperty(ref _isPreClear, value);
		}

		int _selectedCountGPUs;
		public int SelectedCountGPUs
		{
			get => _selectedCountGPUs;
			set => SetProperty(ref _selectedCountGPUs, value, "SelectedCountGPUs");
		}

		private bool _isMakeNormalCFG;
		public bool IsMakeNormalCFG
		{
			get { return _isMakeNormalCFG; }
			set { SetProperty(ref _isMakeNormalCFG, value); }
		}

		private bool _isMakeTinyCFG;
		public bool IsMakeTinyCFG
		{
			get { return _isMakeTinyCFG; }
			set { SetProperty(ref _isMakeTinyCFG, value); }
		}

		public ObservableCollection<int> CountListGPUs { get; private set; }
		public ObservableCollection<string> ListGPUs { get; private set; }
		public FDInfo OFDInfoSelectDataset { get; private set; }

		public FDInfo OFDInfoSelectCFG { get; private set; }
		public FDInfo OFDInfoSelectDataWeight { get; private set; }
		public FDInfo SFDInfoCFG { get; private set; }
		void CmdWork(string input = "wmic path win32_VideoController get name") //"nvidia-smi -L"
		{
			Process proc = Process.Start(new ProcessStartInfo
			{
				FileName = "cmd",
				Arguments = "/c " + input, //chcp 65001 & nvidia-smi -L
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				//RedirectStandardError = true
			});
			ListGPUs.Clear();
			var cmdOut = proc.StandardOutput.ReadToEnd();
			var strTmp = cmdOut.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 1; i < strTmp.Length; i++)
			{
				string gpuname = strTmp[i].Trim();
				var tmpName = gpuname.Split(' ', '(').ToList();
				if (tmpName.Contains("Intel") && tmpName.Contains("UHD")) continue;
				ListGPUs.Add(gpuname);
			}
			if (ListGPUs.Count() > 0)//@
			{
				//Это когда нейронка на клиенте
				//CountListGPUs.Clear(); 
				//int limit = ListGPUs.Count() > 4 ? 4 : ListGPUs.Count();
				//foreach (var i in Enumerable.Range(1, limit))
				//	CountListGPUs.Add(i);
				//SelectedCountGPUs = 1;
			}
			else
				ListGPUs.Add("Нет подходящих видеокарт");

		}


		RelayCommand _comLoadGPU;
		public RelayCommand LoadGPU
		{
			get
			{
				return _comLoadGPU ??
					 (_comLoadGPU = new RelayCommand(act => { CmdWork(); }));
			}
		}

		RelayCommand _comSelectDatasetPath;
		public RelayCommand SelectDatasetPath
		{
			get => _comSelectDatasetPath ??
				(_comSelectDatasetPath = new RelayCommand(_ =>
				{
					StartOpenDialog(OFDInfoSelectDataset);


				}

				));
		}



		RelayCommand _comSelectCFGPath;
		public RelayCommand SelectCFGPath
		{
			get
			{
				return _comSelectCFGPath ?? (_comSelectCFGPath = new RelayCommand(_ =>
				{
					if (StartOpenDialog(OFDInfoSelectCFG))
					{
						_WHLearn = CfgMaker.GetCfgValue(OFDInfoSelectCFG.FilePath, "size");
						RaisePropertyChanged("WHLearn");
						_subdivisions = CfgMaker.GetCfgValue(OFDInfoSelectCFG.FilePath, "subdivisions");
						RaisePropertyChanged("Subdivisions");
					}
				}));
			}
		}


		RelayCommand _comSelectWeight;
		public RelayCommand SelectWeight
		{
			get => _comSelectWeight ??
				(_comSelectWeight = new RelayCommand(_ => StartOpenDialog(OFDInfoSelectDataWeight)));
		}

		bool StartOpenDialog(object ofdInfo)
		{
			var info = ofdInfo as FDInfo;
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = info.Title;
			ofd.Filter = info.Filter;
			ofd.InitialDirectory = info.InitialDirectory;
			if (ofd.ShowDialog() == true)
			{
				if (ofd.FileName != "")
				{
					info.FilePath = ofd.FileName;
					return true;
				}
				return false;

			}
			return false;
		}

		RelayCommand _comCreateCFG;
		public RelayCommand CreateCFG
		{
			get
			{
				return _comCreateCFG ?? (_comCreateCFG = new RelayCommand(_ =>
				{
					if (StartSaveDialog(SFDInfoCFG))
					{
						cfgMaker = new CfgMaker(TemplateNormalCFGPath, OFDInfoSelectDataset.FilePath, SFDInfoCFG.FilePath);
						cfgMaker.CreateNewCfg();
					}
				},
				obj => OFDInfoSelectDataset.FilePath != null
				));
			}
		}

		RelayCommand _comCreateTinyCFG;
		public RelayCommand CreateTinyCFG
		{
			get
			{
				return _comCreateTinyCFG ?? (_comCreateTinyCFG = new RelayCommand(_ =>
				{
					if (StartSaveDialog(SFDInfoCFG))
					{
						cfgMaker = new CfgMaker(TemplateTinyCFGPath, OFDInfoSelectDataset.FilePath, SFDInfoCFG.FilePath);
						cfgMaker.CreateNewCfg();
					}
				},
				obj => OFDInfoSelectDataset.FilePath != null
				));
			}
		}
		bool StartSaveDialog(object sfdInfo)
		{
			var info = sfdInfo as FDInfo;
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Title = info.Title;
			sfd.Filter = info.Filter;
			sfd.InitialDirectory = info.InitialDirectory;
			if (sfd.ShowDialog() == true)
			{
				if (sfd.FileName != "")
				{
					info.FilePath = sfd.FileName;
					return true;

				}
			}
			return false;
		}

		RelayCommand _comStartLearn;
		public RelayCommand StartLearn
		{
			get
			{
				return _comStartLearn ?? (_comStartLearn = new RelayCommand(_ =>
				{

					RabbitSender.SendStart(OFDInfoSelectDataset.FilePath,
					OFDInfoSelectCFG.FilePath, OFDInfoSelectDataWeight.FilePath, IsNeedmAP, IsPreClear, SelectedCountGPUs);
				},
					_ => OFDInfoSelectDataset.FilePath != null && OFDInfoSelectCFG.FilePath != null && OFDInfoSelectDataWeight.FilePath != null && CountListGPUs.Count != 0));
			}
		}

		RelayCommand _stop;
		public RelayCommand StopLearn
		{
			get => _stop ?? (_stop = new RelayCommand(_ =>

			RabbitSender.SendStop()

			));
		}

		private RabbitSender RabbitSender { get; set; }

		private string _rabbitConnection;
		public string RabbitConnection
		{
			get { return _rabbitConnection; }
			set {
				RabbitSender.Connection = value;
				SetProperty(ref _rabbitConnection, value);
			}
		}

		private string _log="";
		public string Log
		{
			get { return _log; }
			set
			{
				SetProperty(ref _log, value);
			}
		}

		public void WriteLogFile(FilePath model)
		{
			string message = "[" + model.Time.ToString("dd.MM HH:mm") + "]" + " Created " + model.Name;
			Log += message + '\n';
		}

		RelayCommand _comGetmAP;
		public RelayCommand GetmAP
		{
			get
			{
				return _comGetmAP ?? (_comGetmAP = new RelayCommand(_ =>
				{
					var fdinfo = new FDInfo("Выберите файл весов для вычисления mAP при текущей концигурации нейронной сети", "Weight files(*.weights)|*.weights| All files(*.*) | *.*", maidNeuralNetDir.FullName + "\\backup");
					if (StartOpenDialog(fdinfo))
					{
						var strmAP = RabbitSender.GetMAP(OFDInfoSelectDataset.FilePath, OFDInfoSelectCFG.FilePath, fdinfo.FilePath);
						MessageBox.Show(strmAP, "mAP для " + fdinfo.FilePath);
					}
				},
					_ => OFDInfoSelectDataset.FilePath != null && OFDInfoSelectCFG.FilePath != null));
			}
		}
	}
}
