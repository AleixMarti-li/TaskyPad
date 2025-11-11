using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Velopack;

namespace TaskyPad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {

        [STAThread]
        private static void Main(string[] args)
        {
            VelopackApp.Build().Run();
            App app = new();
            app.InitializeComponent();
            app.Run();
        }

        private static Mutex? mutex;
        private static NamedPipeServerStream? pipeServer;
        private const string PipeName = "TaskyPadAppPipe";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                bool nuevaInstancia;
                mutex = new Mutex(true, "TaskyPadAppSingleton", out nuevaInstancia);

                if (!nuevaInstancia)
                {
                    // Si ya existe una instancia, enviamos un mensaje por Named Pipe
                    EnviarMensajeAInstanciaExistente();
                    System.Windows.Application.Current.Shutdown();
                    return;
                }

                // Si es la primera instancia, iniciamos el servidor de Named Pipe
                IniciarServidorNamedPipe();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en OnStartup: {ex.Message}");
                System.Windows.MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }

        private static void EnviarMensajeAInstanciaExistente()
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    // Esperar a que se conecte
                    pipeClient.Connect(1000); // 1 segundo de timeout
                    byte[] mensaje = Encoding.UTF8.GetBytes("SHOW_WINDOW");
                    pipeClient.Write(mensaje, 0, mensaje.Length);
                    pipeClient.Flush();
                    Debug.WriteLine("Mensaje enviado exitosamente a la instancia existente");
                }
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("Timeout al conectar con la instancia existente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al enviar mensaje: {ex.Message}");
            }
        }

        private static void IniciarServidorNamedPipe()
        {
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        try
                        {
                            pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);
                            pipeServer.WaitForConnection();

                            byte[] buffer = new byte[256];
                            int bytesLeidos = pipeServer.Read(buffer, 0, buffer.Length);
                            string mensaje = Encoding.UTF8.GetString(buffer, 0, bytesLeidos);

                            Debug.WriteLine($"Mensaje recibido: {mensaje}");

                            if (mensaje == "SHOW_WINDOW")
                            {
                                // Ejecutamos en el hilo principal de la UI
                                Current?.Dispatcher?.Invoke(() =>
                                {
                                    try
                                    {
                                        if (Current?.MainWindow is MainWindow mainWindow)
                                        {
                                            mainWindow.Show();
                                            mainWindow.WindowState = System.Windows.WindowState.Normal;
                                            mainWindow.Activate();
                                            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                                            Debug.WriteLine("Ventana mostrada exitosamente");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error mostrando ventana: {ex.Message}");
                                    }
                                });
                            }

                            pipeServer?.Disconnect();
                            pipeServer?.Dispose();
                            pipeServer = null;
                        }
                        catch (OperationCanceledException)
                        {
                            Debug.WriteLine("Named Pipe cancelado");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error en bucle Named Pipe: {ex.Message}");
                            pipeServer?.Dispose();
                            pipeServer = null;
                            // Esperamos un poco antes de reintentar
                            await Task.Delay(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error en servidor Named Pipe: {ex.Message}");
                }
            });
        }
    }
}
