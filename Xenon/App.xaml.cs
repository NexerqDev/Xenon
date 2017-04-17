using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Xenon
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool CompatFlag = false;
        public static Mutex mutex;

        [STAThread]
        public static void Main(string[] args)
        {
            mutex = new Mutex(true, "NexerqDev.Xenon-MapleLAUNCHER");
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                if (args.Length > 0 && args[0] == "-compat")
                {
                    if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
                    {
                        MessageBox.Show("Please run Xenon as Administrator as you are in compatibility mode!", "Xenon", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    CompatFlag = true; // for some reason, i was trying this on another win7 and shellexecuteex wasnt working properly.
                }

                var app = new App();
                app.Run(new MainWindow());
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show("Only one instance of Xenon can be running at once!", "Xenon", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
