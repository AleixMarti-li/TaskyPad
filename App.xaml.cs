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
using Serilog;

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
            // Inicializar Serilog antes de cualquier otra cosa
            LoggingService.InitializeLogger();
            Log.Information("Main method called with {ArgCount} arguments", args.Length);
            
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
            Log.Information("==================================================");
            Log.Information("Application OnStartup called");
            Log.Information("==================================================");
            Log.Information("Startup arguments: {Arguments}", string.Join(", ", e.Args));
            Log.Information("Command line arguments count: {Count}", _commandLineArgs.Length);
            
            try
            {
                bool nuevaInstancia;
                mutex = new Mutex(true, "TaskyPadAppSingleton", out nuevaInstancia);
                
                Log.Information("Mutex creation result - Is new instance: {IsNewInstance}", nuevaInstancia);

                if (!nuevaInstancia)
                {
                    Log.Information("Another instance is already running, sending message to existing instance");
                    // Si ya existe una instancia, enviamos un mensaje por Named Pipe
                    EnviarMensajeAInstanciaExistente();
                    Log.Information("Message sent, shutting down this instance");
                    System.Windows.Application.Current.Shutdown();
                    return;
                }

                Log.Information("This is the first instance, proceeding with startup");

                // Si es la primera instancia, iniciamos el servidor de Named Pipe
                IniciarServidorNamedPipe();

                // Check for -silent argument
                bool isSilentMode = HasArgument("-silent");
                Log.Information("Silent mode argument present: {IsSilentMode}", isSilentMode);
                
                // Si se pasó el argumento -silent, no mostrar la ventana principal
                if (isSilentMode)
                {
                    Log.Information("===== SILENT MODE DETECTED =====");
                    Log.Information("Application started in silent mode (-silent)");
                    Debug.WriteLine("Aplicación iniciada en modo silencioso (-silent)");
                    
                    // Crear la MainWindow pero sin mostrarla
                    Log.Information("Creating MainWindow in hidden state");
                    MainWindow mainWindow = new MainWindow();
                    
                    Log.Information("Setting MainWindow visibility to Hidden");
                    mainWindow.Visibility = Visibility.Hidden;
                    
                    Log.Information("Setting ShowInTaskbar to false");
                    mainWindow.ShowInTaskbar = false;
                    
                    this.MainWindow = mainWindow;
                    
                    Log.Information("Silent mode MainWindow configuration:");
                    Log.Information("  - Visibility: {Visibility}", mainWindow.Visibility);
                    Log.Information("  - WindowState: {WindowState}", mainWindow.WindowState);
                    Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", mainWindow.ShowInTaskbar);
                    Log.Information("  - IsLoaded: {IsLoaded}", mainWindow.IsLoaded);
                    Log.Information("  - IsVisible: {IsVisible}", mainWindow.IsVisible);
                    Log.Information("===== END SILENT MODE SETUP =====");
                }
                else
                {
                    Log.Information("===== NORMAL MODE DETECTED =====");
                    Log.Information("Starting application in normal mode (window visible)");
                    
                    // Mostrar la MainWindow normalmente
                    Log.Information("Creating MainWindow");
                    MainWindow mainWindow = new MainWindow();
                    
                    Log.Information("Calling Show() on MainWindow");
                    mainWindow.Show();
                    
                    this.MainWindow = mainWindow;
                    
                    Log.Information("Normal mode MainWindow configuration:");
                    Log.Information("  - Visibility: {Visibility}", mainWindow.Visibility);
                    Log.Information("  - WindowState: {WindowState}", mainWindow.WindowState);
                    Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", mainWindow.ShowInTaskbar);
                    Log.Information("  - IsLoaded: {IsLoaded}", mainWindow.IsLoaded);
                    Log.Information("  - IsVisible: {IsVisible}", mainWindow.IsVisible);
                    Log.Information("  - ActualWidth: {Width}", mainWindow.ActualWidth);
                    Log.Information("  - ActualHeight: {Height}", mainWindow.ActualHeight);
                    Log.Information("===== END NORMAL MODE SETUP =====");
                }

                // Procesar otros argumentos de línea de comandos
                ProcessCommandLineArguments();

                // Registrar el manejador de activaciones de Toast notifications
                Log.Information("Registering Toast notification handler");
                ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

                // Limpiar logs antiguos (opcional)
                Log.Information("Cleaning old log files (keeping last 7 days)");
                LoggingService.CleanOldLogs(7); // Mantener logs de los últimos 7 días

                Log.Information("Calling base.OnStartup");
                base.OnStartup(e);
                
                Log.Information("==================================================");
                Log.Information("OnStartup completed successfully");
                Log.Information("Application is now running");
                Log.Information("==================================================");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical error in OnStartup");
                Debug.WriteLine($"Error en OnStartup: {ex.Message}");
                System.Windows.MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            Log.Information("Application activated");
            base.OnActivated(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            Log.Information("Application deactivated");
            base.OnDeactivated(e);
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            Log.Warning("System session ending - Reason: {Reason}", e.ReasonSessionEnding);
            base.OnSessionEnding(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exiting with code {ExitCode}", e.ApplicationExitCode);
            
            try
            {
                // Limpiar recursos
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }

                if (pipeServer != null)
                {
                    pipeServer.Dispose();
                }

                ToastNotificationManagerCompat.Uninstall();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during application exit cleanup");
            }
            finally
            {
                // Cerrar el logger
                LoggingService.CloseLogger();
            }

            base.OnExit(e);
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
                Log.Information("Processing command line arguments: {Arguments}", string.Join(", ", GetCommandLineArgs()));
                
                // Otros argumentos pueden ser procesados aquí
                string? config = GetArgumentValue("-config");
                if (!string.IsNullOrEmpty(config))
                {
                    Log.Information("Configuration specified: {Config}", config);
                    Debug.WriteLine($"Configuración especificada: {config}");
                }

                Debug.WriteLine($"Argumentos de línea de comandos: {string.Join(", ", GetCommandLineArgs())}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing command line arguments");
                Debug.WriteLine($"Error procesando argumentos de línea de comandos: {ex.Message}");
            }
        }

        private static void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // Manejar la activación de la notificación Toast
            try
            {
                Log.Information("Toast notification activated with argument: {Argument}", e.Argument);
                
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
                    Log.Debug("Task clicked: {TaskId}", taskId);
                    Debug.WriteLine($"Task clicked: {taskId}");
                }

                if (args.Contains("action"))
                {
                    action = args["action"];
                    Log.Debug("Action: {Action}", action);
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
                Log.Error(ex, "Error handling Toast notification activation");
                Debug.WriteLine($"Error manejando activación de Toast: {ex.Message}");
            }
        }

        private static void CallDispatchShowProgram()
        {
            Log.Information("===== CallDispatchShowProgram invoked =====");
            Log.Information("Attempting to show and activate main window");
            
            Current?.Dispatcher?.Invoke(() =>
            {
                try
                {
                    if (Current?.MainWindow is MainWindow mainWindow)
                    {
                        Log.Information("MainWindow found, current state:");
                        Log.Information("  - Visibility: {Visibility}", mainWindow.Visibility);
                        Log.Information("  - WindowState: {WindowState}", mainWindow.WindowState);
                        Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", mainWindow.ShowInTaskbar);
                        Log.Information("  - IsVisible: {IsVisible}", mainWindow.IsVisible);
                        
                        Log.Information("Calling Show()");
                        mainWindow.Show();
                        
                        Log.Information("Setting WindowState to Normal");
                        mainWindow.WindowState = System.Windows.WindowState.Normal;
                        
                        Log.Information("Setting ShowInTaskbar to true");
                        mainWindow.ShowInTaskbar = true;
                        
                        Log.Information("Calling Activate()");
                        mainWindow.Activate();
                        
                        IntPtr mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
                        Log.Information("Process MainWindowHandle: {Handle}", mainWindowHandle);
                        
                        if (mainWindowHandle != IntPtr.Zero)
                        {
                            Log.Information("Calling SetForegroundWindow");
                            SetForegroundWindow(mainWindowHandle);
                        }
                        else
                        {
                            Log.Warning("MainWindowHandle is null/zero, cannot call SetForegroundWindow");
                        }
                        
                        Log.Information("Window restoration complete - final state:");
                        Log.Information("  - Visibility: {Visibility}", mainWindow.Visibility);
                        Log.Information("  - WindowState: {WindowState}", mainWindow.WindowState);
                        Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", mainWindow.ShowInTaskbar);
                        Log.Information("  - IsVisible: {IsVisible}", mainWindow.IsVisible);
                        Log.Information("  - IsActive: {IsActive}", mainWindow.IsActive);
                        
                        Log.Information("Main window shown and activated successfully");
                        Debug.WriteLine("Ventana mostrada exitosamente");
                    }
                    else
                    {
                        Log.Warning("MainWindow is null, cannot show window");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error showing main window");
                    Debug.WriteLine($"Error mostrando ventana: {ex.Message}");
                }
            });
            
            Log.Information("===== CallDispatchShowProgram completed =====");
        }

        private static void EnviarMensajeAInstanciaExistente()
        {
            try
            {
                Log.Information("Sending message to existing instance");
                
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    // Esperar a que se conecte
                    pipeClient.Connect(1000); // 1 segundo de timeout
                    byte[] mensaje = Encoding.UTF8.GetBytes("SHOW_WINDOW");
                    pipeClient.Write(mensaje, 0, mensaje.Length);
                    pipeClient.Flush();
                    Log.Information("Message sent successfully to existing instance");
                    Debug.WriteLine("Mensaje enviado exitosamente a la instancia existente");
                }
            }
            catch (TimeoutException ex)
            {
                Log.Warning(ex, "Timeout connecting to existing instance");
                Debug.WriteLine("Timeout al conectar con la instancia existente");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending message to existing instance");
                Debug.WriteLine($"Error al enviar mensaje: {ex.Message}");
            }
        }

        private static void IniciarServidorNamedPipe()
        {
            Log.Information("Starting Named Pipe server");
            
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

                            Log.Information("Named Pipe message received: {Message}", mensaje);
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
                            Log.Information("Named Pipe cancelled");
                            Debug.WriteLine("Named Pipe cancelado");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error in Named Pipe loop");
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
                    Log.Fatal(ex, "Critical error in Named Pipe server");
                    Debug.WriteLine($"Error en servidor Named Pipe: {ex.Message}");
                }
            });
        }
    }
}
