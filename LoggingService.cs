using Serilog;
using Serilog.Core;
using System;
using System.IO;

namespace TaskyPad
{
    public static class LoggingService
    {
        private static Logger? _logger;

        /// <summary>
        /// Inicializa el logger de Serilog con un archivo único por cada ejecución.
        /// </summary>
        public static void InitializeLogger()
        {
            try
            {
                // Ruta de la carpeta de logs
                string logsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TaskyPad",
                    "Logs"
                );

                // Crear el directorio si no existe
                if (!Directory.Exists(logsPath))
                {
                    Directory.CreateDirectory(logsPath);
                }

                // Nombre del archivo con timestamp de inicio de la aplicación
                string logFileName = $"taskypad_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                string logFilePath = Path.Combine(logsPath, logFileName);

                // Configurar Serilog
                _logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(
                        logFilePath,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                        rollingInterval: RollingInterval.Infinite, // No rotar, cada ejecución tiene su archivo
                        retainedFileCountLimit: 30, // Mantener solo los últimos 30 archivos
                        shared: false,
                        flushToDiskInterval: TimeSpan.FromSeconds(1)
                    )
                    .CreateLogger();

                Log.Logger = _logger;

                Log.Information("=== TaskyPad Application Started ===");
                Log.Information("Log file: {LogFilePath}", logFilePath);
                Log.Information("Application Version: {Version}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            }
            catch (Exception ex)
            {
                // Si falla la inicialización del logger, al menos registramos en el debug
                System.Diagnostics.Debug.WriteLine($"Error al inicializar Serilog: {ex.Message}");
            }
        }

        /// <summary>
        /// Cierra y libera el logger.
        /// </summary>
        public static void CloseLogger()
        {
            try
            {
                Log.Information("=== TaskyPad Application Closed ===");
                Log.CloseAndFlush();
                _logger?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar Serilog: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpia los archivos de log antiguos, manteniendo solo los últimos N días.
        /// </summary>
        /// <param name="daysToKeep">Número de días de logs a mantener (por defecto 7)</param>
        public static void CleanOldLogs(int daysToKeep = 7)
        {
            try
            {
                string logsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TaskyPad",
                    "Logs"
                );

                if (!Directory.Exists(logsPath))
                    return;

                var files = Directory.GetFiles(logsPath, "taskypad_*.log");
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            Log.Information("Deleted old log file: {FileName}", fileInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Could not delete old log file: {FileName}", fileInfo.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error cleaning old log files");
            }
        }
    }
}
