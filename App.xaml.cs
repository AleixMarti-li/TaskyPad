using Microsoft.Toolkit.Uwp.Notifications;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Velopack;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Collections;

namespace TaskyPad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static string[] _commandLineArgs = Array.Empty<string>();

        [STAThread]
        private static void Main(string[] args)
        {
            _commandLineArgs = args;
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

                // Si se pasó el argumento -silent, no mostrar la ventana principal
                if (HasArgument("-silent"))
                {
                    Debug.WriteLine("Aplicación iniciada en modo silencioso (-silent)");
                    
                    // Crear la MainWindow pero sin mostrarla
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Visibility = Visibility.Hidden;
                    mainWindow.ShowInTaskbar = false;
                    this.MainWindow = mainWindow;
                }
                else
                {
                    // Mostrar la MainWindow normalmente
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.MainWindow = mainWindow;
                }

                // Procesar otros argumentos de línea de comandos
                ProcessCommandLineArguments();

                // Registrar el manejador de activaciones de Toast notifications
                ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en OnStartup: {ex.Message}");
                System.Windows.MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Recupera los argumentos de línea de comandos pasados a la aplicación.
        /// </summary>
        /// <returns>Array con los argumentos de línea de comandos.</returns>
        public static string[] GetCommandLineArgs()
        {
            return _commandLineArgs ?? Array.Empty<string>();
        }

        /// <summary>
        /// Verifica si un argumento específico está presente en los argumentos de línea de comandos.
        /// </summary>
        /// <param name="argumentName">Nombre del argumento a buscar (ej: "-silent", "--verbose")</param>
        /// <returns>true si el argumento está presente, false en caso contrario.</returns>
        public static bool HasArgument(string argumentName)
        {
            return GetCommandLineArgs().Any(arg => arg.Equals(argumentName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Obtiene el valor de un argumento específico.
        /// </summary>
        /// <param name="argumentName">Nombre del argumento a buscar (ej: "-config", "--output")</param>
        /// <param name="defaultValue">Valor por defecto si no se encuentra el argumento.</param>
        /// <returns>El valor del argumento o el valor por defecto.</returns>
        public static string? GetArgumentValue(string argumentName, string? defaultValue = null)
        {
            var args = GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        return args[i + 1];
                    }
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Procesa los argumentos de la aplicación y realiza las acciones correspondientes.
        /// </summary>
        private void ProcessCommandLineArguments()
        {
            try
            {
                // Otros argumentos pueden ser procesados aquí
                string? config = GetArgumentValue("-config");
                if (!string.IsNullOrEmpty(config))
                {
                    Debug.WriteLine($"Configuración especificada: {config}");
                }

                Debug.WriteLine($"Argumentos de línea de comandos: {string.Join(", ", GetCommandLineArgs())}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error procesando argumentos de línea de comandos: {ex.Message}");
            }
        }

        private static void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // Manejar la activación de la notificación Toast
            try
            {
                ToastArguments args = ToastArguments.Parse(e.Argument);

                // Obtener la entrada del usuario (cuadros de texto, selecciones de menú)
                ValueSet userInput = e.UserInput;

                Debug.WriteLine($"Toast notification activated with argument: {e.Argument}");

                // TODO: Mostrar el contenido correspondiente basado en los argumentos
                // Por ejemplo, si tienes un taskId en los argumentos, puedes navegar a esa tarea
                string taskId = string.Empty;
                string action = string.Empty;
                if (args.Contains("taskId"))
                {
                    taskId = args["taskId"];
                    Debug.WriteLine($"Task clicked: {taskId}");
                }

                if (args.Contains("action"))
                {
                    action = args["action"];
                    Debug.WriteLine($"action: {action}");
                    
                    // Usar la instancia de TaskService de MainWindow en lugar de crear una nueva
                    Current?.Dispatcher?.Invoke(() =>
                    {
                        if (Current?.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.ExecuteTaskAction(taskId, action);
                        }
                    });
                }

                if (!(string.IsNullOrEmpty(taskId)) && (string.IsNullOrEmpty(action)))
                {
                    CallDispatchShowProgram();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error manejando activación de Toast: {ex.Message}");
            }
        }

        private static void CallDispatchShowProgram()
        {
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
                                CallDispatchShowProgram();
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
