using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Security.Cryptography;
using Serilog;

namespace TaskyPad
{
    public class TaskService
    {
        public List<Tarea> _listaTareas = new List<Tarea>();
        private Dictionary<string, System.Timers.Timer> tasksDictionary = new Dictionary<string, System.Timers.Timer>();
        private NotificationService _notificationService;
        private ConfigService _configService;
        private MainWindow _mainWindow;
        public TaskService(MainWindow mainWindow, NotificationService notificationService, ConfigService configService)
        {
            _notificationService = notificationService;
            _configService = configService;
            _mainWindow = mainWindow;
        }

        public void ExecuteAction(string taskId, string action)
        {
            Log.Information("ExecuteAction called for task {TaskId} with action {Action}", taskId, action);
            
            Tarea? TareaSeleccionada = _listaTareas.FirstOrDefault(t => t.idTarea == taskId);
            if (TareaSeleccionada is null)
            {
                Log.Warning("Task {TaskId} not found in ExecuteAction", taskId);
                return;
            }

            if (action == "done")
            {
                Log.Information("Marking task {TaskId} as done", taskId);
                ChangeTaskStatus(TareaSeleccionada, EstadoTarea.Done);
                return;
            }

            if (action.Contains("posponer"))
            {
                string[] partesPosponer = action.Split('=');
                if (partesPosponer.Length != 2)
                {
                    Log.Warning("Invalid postpone action format: {Action}", action);
                    return;
                }

                string timeStr = partesPosponer[1];
                int time;
                if (!int.TryParse(timeStr, out time))
                {
                    Log.Warning("Invalid time value in postpone action: {TimeStr}", timeStr);
                    return;
                }

                Log.Information("Postponing task {TaskId} by {Minutes} minutes", taskId, time);
                TareaSeleccionada.fecha = DateTime.Now.AddMinutes(time);

                UpdateTarea(TareaSeleccionada);
            }
        }

        public void InicializeTimers()
        {
            Log.Information("Initializing timers for all tasks");
            LoadTareasInternos();
            if (_listaTareas is null || _listaTareas.Count == 0)
            {
                Log.Information("No tasks found to initialize timers");
                return;
            }
            
            Log.Information("Creating timers for {TaskCount} tasks", _listaTareas.Count);
            foreach (Tarea teareaIndividual in _listaTareas)
            {
                CreateTimer(teareaIndividual);
            }
        }

        public void CreateTimer(Tarea tareaSelecionada)
        {
            if (!tareaSelecionada.notificar)
            {
                Log.Debug("Task {TaskId} ({TaskTitle}) does not require notifications, skipping timer creation", 
                    tareaSelecionada.idTarea, tareaSelecionada.titulo);
                return;
            }

            DateTime ahoraActual = DateTime.Now;
            DateTime objetivo = tareaSelecionada.fecha;

            double diferencial = (objetivo - ahoraActual).TotalMilliseconds;

            if (diferencial < 0)
            {
                Log.Debug("Task {TaskId} ({TaskTitle}) target date is in the past, skipping timer creation", 
                    tareaSelecionada.idTarea, tareaSelecionada.titulo);
                return;
            }

            System.Timers.Timer timer = new System.Timers.Timer(diferencial);
            timer.Elapsed += (sender, e) => HandleTimerElapsed(sender, e, tareaSelecionada);
            timer.AutoReset = false;
            timer.Start();

            Log.Information("Created timer for task {TaskId} ({TaskTitle}) that will fire in {Milliseconds}ms at {TargetTime}", 
                tareaSelecionada.idTarea, tareaSelecionada.titulo, diferencial, objetivo);
            Console.WriteLine($"Creado timer que se inicia en {diferencial}");
            tasksDictionary.TryAdd(tareaSelecionada.idTarea, timer);
        }

        public void EditTimer(Tarea tareaSelecionada)
        {
            Log.Information("Editing timer for task {TaskId} ({TaskTitle})", 
                tareaSelecionada.idTarea, tareaSelecionada.titulo);
            
            if (!tareaSelecionada.notificar)
            {
                Log.Debug("Notifications disabled for task {TaskId}, deleting timer", tareaSelecionada.idTarea);
                DeleteTimer(tareaSelecionada);
                return;
            }

            DateTime ahoraActual = DateTime.Now;
            DateTime objetivo = tareaSelecionada.fecha;

            double diferencial = (objetivo - ahoraActual).TotalMilliseconds;

            System.Timers.Timer? timerRecuperado;
            if (!tasksDictionary.TryGetValue(tareaSelecionada.idTarea, out timerRecuperado))
            {
                Log.Debug("Timer not found for task {TaskId}, creating new timer", tareaSelecionada.idTarea);
                CreateTimer(tareaSelecionada);
                return;
            }

            Log.Debug("Recreating timer for task {TaskId}", tareaSelecionada.idTarea);
            DeleteTimer(tareaSelecionada);
            CreateTimer(tareaSelecionada);
        }

        public void DeleteTimer(Tarea tareaSelecionada)
        {
            Log.Information("Deleting timer for task {TaskId} ({TaskTitle})", 
                tareaSelecionada.idTarea, tareaSelecionada.titulo);
            
            System.Timers.Timer? timerRecuperado;
            if (!tasksDictionary.TryGetValue(tareaSelecionada.idTarea, out timerRecuperado))
            {
                Log.Debug("Timer not found for task {TaskId}", tareaSelecionada.idTarea);
                return;
            }

            timerRecuperado.Stop();
            tasksDictionary.Remove(tareaSelecionada.idTarea);
            Log.Debug("Timer stopped and removed for task {TaskId}", tareaSelecionada.idTarea);
        }

        private void HandleTimerElapsed(object? sender, ElapsedEventArgs e, Tarea tareaEjecutada)
        {
            Log.Information("Timer elapsed for task {TaskId} ({TaskTitle}) at {ElapsedTime}", 
                tareaEjecutada.idTarea, tareaEjecutada.titulo, e.SignalTime);
            
            //recuperar tarea
            if (tareaEjecutada.estado == EstadoTarea.Done)
            {
                Log.Debug("Task {TaskId} is already done, skipping notification", tareaEjecutada.idTarea);
                return;
            }
            
            _notificationService.SendWindowsTaskNotificacionEndTime(tareaEjecutada);
        }

        public void UpdateTarea(Tarea updatedTarea, MainWindow? mainWindow = null)
        {
            Log.Information("Updating task {TaskId} ({TaskTitle})", updatedTarea.idTarea, updatedTarea.titulo);
            
            var tareaExistente = _listaTareas.Find(t => t.idTarea == updatedTarea.idTarea);
            if (tareaExistente != null)
            {
                tareaExistente.titulo = updatedTarea.titulo;
                tareaExistente.descripcion = updatedTarea.descripcion;
                tareaExistente.fecha = updatedTarea.fecha;
                tareaExistente.notificar = updatedTarea.notificar;
                tareaExistente.ultimaModificacion = DateTime.Now;
                
                SaveTareas(_listaTareas);
                EditTimer(tareaExistente);

                if (mainWindow is not null)
                {
                    mainWindow.RecuperarTareasUI();
                    return;
                } else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Aquí debes obtener la ventana actual si quieres refrescar algo
                        var win = System.Windows.Application.Current.MainWindow as MainWindow;
                        win?.RecuperarTareasUI();
                    });
                }
                
                Log.Information("Task {TaskId} updated successfully", updatedTarea.idTarea);
            }
            else
            {
                Log.Warning("Task {TaskId} not found for update", updatedTarea.idTarea);
            }
        }

        public void DeleteTarea(Tarea tareaEliminada, MainWindow mainWindow)
        {
            Log.Information("Deleting task {TaskId} ({TaskTitle})", tareaEliminada.idTarea, tareaEliminada.titulo);
            
            _listaTareas.Remove(tareaEliminada);
            DeleteTimer(tareaEliminada);
            SaveTareas(_listaTareas);
            mainWindow.RecuperarTareasUI();
            
            Log.Information("Task {TaskId} deleted successfully", tareaEliminada.idTarea);
        }

        public void ChangeTaskStatus(Tarea tarea, EstadoTarea nuevoEstado, MainWindow? mainWindow = null)
        {
            Log.Information("Changing status of task {TaskId} ({TaskTitle}) from {OldStatus} to {NewStatus}", 
                tarea.idTarea, tarea.titulo, tarea.estado, nuevoEstado);
            
            var tareaExistente = _listaTareas.Find(t => t.idTarea == tarea.idTarea);
            if (tareaExistente != null)
            {
                tareaExistente.estado = nuevoEstado;
                tareaExistente.ultimaModificacion = DateTime.Now;
                SaveTareas(_listaTareas);

                if (mainWindow is not null)
                {
                    mainWindow.RecuperarTareasUI();
                    return;
                } else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Aquí debes obtener la ventana actual si quieres refrescar algo
                        var win = System.Windows.Application.Current.MainWindow as MainWindow;
                        win?.RecuperarTareasUI();
                    });
                }
                
                Log.Information("Task {TaskId} status changed successfully", tarea.idTarea);
            }
            else
            {
                Log.Warning("Task {TaskId} not found for status change", tarea.idTarea);
            }
        }

        public List<Tarea>? LoadTareas(MainWindow? mainWindow = null)
        {
            Log.Information("Loading tasks");
            LoadTareasInternos();
            if (mainWindow is not null) mainWindow.RecuperarTareasUI();
            Log.Information("Loaded {TaskCount} tasks", _listaTareas?.Count ?? 0);
            return _listaTareas;
        }

        public string LoadTareasPath()
        {
            string folder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TaskyPad",
                "Tasks"
            );
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, "tasks.tpf");
        }

        private void LoadTareasInternos()
        {
            Log.Information("Loading tasks from file: {FilePath}", LoadTareasPath());
            
            string? RecoveryKey = null;
            bool EncryptEnabled = false;
            if (_configService is not null)
            {
                if (_configService._configuracion.enableEncrypt)
                {
                    EncryptEnabled = true;
                    RecoveryKey = _configService._configuracion.passwordEncrypt;
                    Log.Information("Encryption is enabled for tasks");
                }
            }

            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists(LoadTareasPath()))
            {
                using (File.Create(LoadTareasPath())) { }
                Log.Information("Tasks file did not exist, created new empty file");
            }
            
            string conteindoJSON = File.ReadAllText(LoadTareasPath());
            if (string.IsNullOrEmpty(conteindoJSON))
            {
                Log.Information("Tasks file is empty, initializing empty task list");
                _listaTareas = new List<Tarea>();
                return;
            }

            if (EncryptEnabled && RecoveryKey is not null && _configService?._configuracion.passwordEncrypt is not null)
            {
                Log.Debug("Attempting to decrypt tasks");
                if(Crypto.Decrypt(conteindoJSON, _configService._configuracion.passwordEncrypt, out string? Result))
                {
                    conteindoJSON = Result ?? string.Empty;
                    Log.Information("Tasks decrypted successfully");
                } else
                {
                    Log.Error("Failed to decrypt tasks - incorrect password");
                    //show error decrypt
                    CustomMessageBox.ShowConfirmation(_mainWindow, "Error al descifrar las tareas. Es posible que la contraseña de cifrado sea incorrecta.", "Error de Descifrado", CustomMessageBoxButton.OK);
                    _listaTareas = new List<Tarea>();
                    return;
                }
            }

            bool isJson = conteindoJSON.TrimStart().StartsWith("{") || conteindoJSON.TrimStart().StartsWith("[");

            if (!isJson && !EncryptEnabled) 
            {
                Log.Error("Tasks file content is not valid JSON and encryption is not enabled");
                CustomMessageBox.ShowConfirmation(_mainWindow, "Error al descifrar las tareas. Es posible que la contraseña de cifrado sea incorrecta.", "Error de Descifrado", CustomMessageBoxButton.OK);
                _listaTareas = new List<Tarea>();
                return;
            }

            try
            {
                List<Tarea>? tareasRecuperadas = JsonSerializer.Deserialize<List<Tarea>>(conteindoJSON);
                _listaTareas = tareasRecuperadas ?? new List<Tarea>();
                Log.Information("Successfully deserialized {TaskCount} tasks", _listaTareas.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deserializing tasks from JSON");
                _listaTareas = new List<Tarea>();
            }
        }

        public void SaveTareas(List<Tarea> tareas)
        {
            Log.Information("Saving {TaskCount} tasks to file", tareas.Count);
            
            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists(LoadTareasPath()))
            {
                using (File.Create(LoadTareasPath())) { }
            }
            
            string json = JsonSerializer.Serialize(tareas);

            string? RecoveryKey = null;
            bool EncryptEnabled = false;
            if (_configService is not null)
            {
                if (_configService._configuracion.enableEncrypt)
                {
                    EncryptEnabled = true;
                    RecoveryKey = _configService._configuracion.passwordEncrypt;
                }
            }

            if (EncryptEnabled && RecoveryKey is not null && _configService?._configuracion.passwordEncrypt is not null)
            {
                Log.Debug("Encrypting tasks before saving");
                if (Crypto.Encrypt(json, _configService._configuracion.passwordEncrypt, out string? Result))
                {
                    if (Result is not null)
                    {
                        json = Result;
                        Log.Information("Tasks encrypted successfully");
                    }
                } else
                {
                    Log.Error("Failed to encrypt tasks");
                    // Falló el cifrado, manejar el error según sea necesario
                    CustomMessageBox.ShowConfirmation(_mainWindow, "Error al cifrar las tareas. Los cambios no se guardarán.", "Error de Cifrado", CustomMessageBoxButton.OK);
                    return;
                }
            }

            File.WriteAllText(LoadTareasPath(), json);
            _listaTareas = tareas;
            Log.Information("Tasks saved successfully to {FilePath}", LoadTareasPath());
        }

        public void RechargeTareasUI(List<Tarea>? tareasRecuperadas, MainWindow mainWindow)
        {
            Log.Information("Recharging tasks UI with {TaskCount} tasks", tareasRecuperadas?.Count ?? 0);
            
            if (tareasRecuperadas is null)
            {
                mainWindow.NoTareasPorHacerMessage.Visibility = Visibility.Visible;
                mainWindow.NoTareasEnProgresoMessage.Visibility = Visibility.Visible;
                mainWindow.NoTareasCompletadasMessage.Visibility = Visibility.Visible;
                return;
            }
            _listaTareas = tareasRecuperadas;
            mainWindow.RecuperarTareasUI();
        }
    }
}
