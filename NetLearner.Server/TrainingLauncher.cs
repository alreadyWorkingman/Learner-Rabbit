using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Linq;
using System.Management;

namespace NetLearner.Server
{
	static class TrainingLauncher
	{
		static string startCommand;
		static string calcMapCommand;
		public static Process trainProc { get; private set; }

		public static void Stop()
		{
			if(trainProc is not null)
			KillProcessAndChildrens(trainProc.Id);
		}
		private static void KillProcessAndChildrens(int pid)
		{
			ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
			  ("Select * From Win32_Process Where ParentProcessID=" + pid);
			ManagementObjectCollection processCollection = processSearcher.Get();

			try
			{
				Process proc = Process.GetProcessById(pid);
				if (!proc.HasExited) proc.Kill();
			}
			catch (ArgumentException)
			{
				// Process already exited.
			}

			if (processCollection != null)
			{
				foreach (ManagementObject mo in processCollection)
				{
					KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
				}
			}
		}


		public static void StartTraining(string neuroNetPath, string dataFilePath, string cfgFilePath, string weightPath, bool isNeedMap = false, bool isPreClear = false, int CountGPUs = 1)//@Изменил на Count
		{
			neuroNetPath = ProtectNNPaths(neuroNetPath);

			ControlBackup(dataFilePath);
			dataFilePath = ProtectPaths(dataFilePath);

			cfgFilePath = ProtectPaths(cfgFilePath);
			weightPath = ProtectPaths(weightPath);

			startCommand = $"{neuroNetPath} detector train {dataFilePath} {cfgFilePath} {weightPath}";
			if (isNeedMap)
				startCommand += " -map";
			if (isPreClear)
				startCommand += " -clear";
			if (CountGPUs > 1)//@Изменил на Count
			{
				startCommand += " -gpus 0";
				for (int i = 1; i < CountGPUs; i++)
					startCommand += $", {i}";
			}

			var commInfo = new ProcessStartInfo();
			commInfo.FileName = "cmd.exe";

			commInfo.Arguments = $"/k {startCommand}";
			commInfo.WorkingDirectory = new System.IO.DirectoryInfo(dataFilePath.Trim('\"')).Parent.Parent.FullName;//@Внезапно это важно
			trainProc = Process.Start(commInfo);
		}

		private static void ControlBackup(string datapath)
		{
			var content = File.ReadAllLines(datapath).ToList<string>();

			for (int i = content.Count-1; i >= 0 ; i--)
			{
				var line = content[i].Split('=');
				if (line[0].Trim() == "backup" && line[1].Trim().LastIndexOf('/') == line[1].Trim().Length-1)
				{
					//var MBResult = MessageBox.Show("В файле " + datapath.Split('\\').Last() + " путь для backup оканчивается на знак \"\\\". " +
					//	"Это может помешать сохранению результатов обучения.\nУбрать знак \"\\\" в конце пути backup из файла?", "Знак \"\\\" в backup",
					//	MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
					//if (MBResult == MessageBoxResult.Yes)
					//{
						var newLine = line[0] + '=' + line[1].Remove(line[1].LastIndexOf('/'));
						content[i] = newLine;
						string endTxt = "";

						foreach (var item in content)
							endTxt+= item + '\n';

						File.WriteAllText(datapath, endTxt);
					//}
					//else return;
				}
			}
		}


		private static string ProtectPaths(string path) => (path.Split(' ').Length == 1) ? path : "\"" + path + "\"";

		private static string ProtectNNPaths(string neuroNetPath)//Ну это ну
		{
			string NNP = "";
			foreach (var item in neuroNetPath.Split('\\'))
			{
				string s = (item.IndexOf(' ') != -1) ? '\"' + item + '\"' : item;
				NNP += '\\' + (s);
			}
			return NNP.Substring(1);
		}
		public static string GetMapInfo(string neuroNetPath, string dataFilePath, string cfgFilePath, string weightPath)
		{
			neuroNetPath = ProtectNNPaths(neuroNetPath);
			dataFilePath = ProtectPaths(dataFilePath);
			cfgFilePath = ProtectPaths(cfgFilePath);
			weightPath = ProtectPaths(weightPath);

			calcMapCommand = $"{neuroNetPath} detector map {dataFilePath} {cfgFilePath} {weightPath}";

			var commInfo = new ProcessStartInfo();
			commInfo.FileName = "cmd";
			commInfo.Arguments = $"/c {calcMapCommand}";
			commInfo.WorkingDirectory = new System.IO.DirectoryInfo(dataFilePath.Trim('\"')).Parent.Parent.FullName;

			var proc = new Process();
			proc.StartInfo = commInfo;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.CreateNoWindow = true;
			proc.Start();

			var reader = proc.StandardOutput;
			var mapInfo = reader.ReadToEnd();

			var startInd = mapInfo.IndexOf("mean average precision (mAP@");
			var endInd = mapInfo.IndexOf(Environment.NewLine, startInd);
			mapInfo = mapInfo.Substring(startInd, endInd - startInd);

			return mapInfo;
		}
	}
}
