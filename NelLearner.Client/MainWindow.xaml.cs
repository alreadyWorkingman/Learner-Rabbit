using System.Windows;


namespace NetLearner.Client
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			var vm = new App2ViewModel();
			if (vm.EverithingBad ) this.Close();//Я знаю, что это ужастно 
			else
			this.DataContext = vm;
		}

	}
}
