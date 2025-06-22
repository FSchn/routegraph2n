using System;

namespace ZusiCLIProject.Routegraph2
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            var main = new MainWindow();
            if (args.Length > 0)
            {
                main.ModulOeffnen(args);
			}
            main.ShowDialog();
        }
    }
}
