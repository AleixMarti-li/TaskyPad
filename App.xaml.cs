using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace TaskyPad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static Mutex? mutex;
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override void OnStartup(StartupEventArgs e)
        {
            bool nuevaInstancia;
            mutex = new Mutex(true, "TaskyPadAppSingleton", out nuevaInstancia);

            if (!nuevaInstancia)
            {
                // Si ya existe una instancia, traemos la ventana al frente
                Process procesoActual = Process.GetCurrentProcess();
                foreach (var proceso in Process.GetProcessesByName(procesoActual.ProcessName))
                {
                    if (proceso.Id != procesoActual.Id)
                    {
                        SetForegroundWindow(proceso.MainWindowHandle);
                        break;
                    }
                }

                System.Windows.Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }

}
