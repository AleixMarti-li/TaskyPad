using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace TaskyPad
{
    public class AutoStartService
    {
        private const string REGISTRY_RUN_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "TaskyPad";
        private const string REGISTRY_KEY = "TaskyPad";

        /// <summary>
        /// Habilita que la aplicación se inicie automáticamente con Windows.
        /// </summary>
        /// <returns>true si se habilitó correctamente, false en caso contrario.</returns>
        public bool EnableAutoStart()
        {
            try
            {
                string applicationPath = GetApplicationPath();
                
                if (string.IsNullOrEmpty(applicationPath) || !File.Exists(applicationPath))
                {
                    Debug.WriteLine("Error: No se pudo obtener la ruta de la aplicación.");
                    return false;
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN_PATH, true))
                {
                    if (key != null)
                    {
                        key.SetValue(REGISTRY_KEY, $"\"{applicationPath}\" -silent");
                        Debug.WriteLine("AutoStart habilitado exitosamente.");
                        return true;
                    }
                }

                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Error de permisos al intentar habilitar AutoStart: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inesperado al habilitar AutoStart: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Desactiva que la aplicación se inicie automáticamente con Windows.
        /// </summary>
        /// <returns>true si se desactivó correctamente, false en caso contrario.</returns>
        public bool DisableAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN_PATH, true))
                {
                    if (key != null)
                    {
                        // Verificar si la clave existe antes de eliminarla
                        if (key.GetValue(REGISTRY_KEY) != null)
                        {
                            key.DeleteValue(REGISTRY_KEY, false);
                            Debug.WriteLine("AutoStart desactivado exitosamente.");
                            return true;
                        }
                        else
                        {
                            Debug.WriteLine("La aplicación no estaba registrada en AutoStart.");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Error de permisos al intentar desactivar AutoStart: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inesperado al desactivar AutoStart: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si la aplicación está registrada para iniciarse automáticamente con Windows.
        /// </summary>
        /// <returns>true si está habilitado, false en caso contrario.</returns>
        public bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN_PATH, false))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(REGISTRY_KEY);
                        return value != null;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al verificar estado de AutoStart: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la ruta completa del ejecutable de la aplicación.
        /// </summary>
        /// <returns>La ruta del ejecutable o null si no se puede determinar.</returns>
        private string? GetApplicationPath()
        {
            try
            {
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string? exePath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(dllPath) ?? string.Empty,
                    "TaskyPad.exe"
                );
                
                if (File.Exists(exePath))
                {
                    return exePath;
                }

                // Si no encuentra el .exe en el mismo directorio, intenta con el Process actual
                exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath != null && File.Exists(exePath))
                {
                    return exePath;
                }

                Debug.WriteLine($"No se pudo encontrar el ejecutable. DLL Path: {dllPath}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener la ruta de la aplicación: {ex.Message}");
                return null;
            }
        }
    }
}
