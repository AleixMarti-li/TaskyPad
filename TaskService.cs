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

namespace TaskyPad
{
    public class TaskService
    {
        public List<Tarea> _listaTareas = new List<Tarea>();
        private Dictionary<string, System.Timers.Timer> tasksDictionary = new Dictionary<string, System.Timers.Timer>();
        private NotificationService _notificationService;
        private ConfigService _configService;
        public TaskService(NotificationService notificationService, ConfigService configService)
        {
            _notificationService = notificationService;
            _configService = configService;
        }

        public TaskService()
        {
            _notificationService = new NotificationService();
        }

        public void ExecuteAction(string taskId, string action)
        {
            Tarea? TareaSeleccionada = _listaTareas.FirstOrDefault(t => t.idTarea == taskId);
            if (TareaSeleccionada is null) return;

            if (action == "done")
            {
                ChangeTaskStatus(TareaSeleccionada, EstadoTarea.Done);
                return;
            }

            if (action.Contains("posponer"))
            {
                string[] partesPosponer = action.Split('=');
                if (partesPosponer.Length != 2) return;

                string timeStr = partesPosponer[1];
                int time;
                if (!int.TryParse(timeStr, out time))
                {
                    return;
                }

                TareaSeleccionada.fecha = DateTime.Now.AddMinutes(time);

                UpdateTarea(TareaSeleccionada);
            }
        }

        public void InicializeTimers()
        {
            LoadTareasInternos();
            if (_listaTareas is null || _listaTareas.Count == 0) return;
            foreach (Tarea teareaIndividual in _listaTareas)
            {
                CreateTimer(teareaIndividual);
            }
        }

        public void CreateTimer(Tarea tareaSelecionada)
        {
            if (!tareaSelecionada.notificar) return;

            DateTime ahoraActual = DateTime.Now;
            DateTime objetivo = tareaSelecionada.fecha;

            double diferencial = (objetivo - ahoraActual).TotalMilliseconds;

            if (diferencial < 0) return;

            System.Timers.Timer timer = new System.Timers.Timer(diferencial);
            timer.Elapsed += (sender, e) => HandleTimerElapsed(sender, e, tareaSelecionada);
            timer.AutoReset = false;
            timer.Start();

            Console.WriteLine($"Creado timer que se inicia en {diferencial}");
            tasksDictionary.TryAdd(tareaSelecionada.idTarea, timer);
        }

        public void EditTimer(Tarea tareaSelecionada)
        {
            if (!tareaSelecionada.notificar)
            {
                DeleteTimer(tareaSelecionada);
                return;
            }

            DateTime ahoraActual = DateTime.Now;
            DateTime objetivo = tareaSelecionada.fecha;

            double diferencial = (objetivo - ahoraActual).TotalMilliseconds;

            System.Timers.Timer? timerRecuperado;
            if (!tasksDictionary.TryGetValue(tareaSelecionada.idTarea, out timerRecuperado))
            {
                CreateTimer(tareaSelecionada);
                return;
            }

            DeleteTimer(tareaSelecionada);
            CreateTimer(tareaSelecionada);
        }

        public void DeleteTimer(Tarea tareaSelecionada)
        {
            System.Timers.Timer? timerRecuperado;
            if (!tasksDictionary.TryGetValue(tareaSelecionada.idTarea, out timerRecuperado))
            {
                return;
            }

            timerRecuperado.Stop();
            tasksDictionary.Remove(tareaSelecionada.idTarea);
        }

        private void HandleTimerElapsed(object? sender, ElapsedEventArgs e, Tarea tareaEjecutada)
        {
            //recuperar tarea
            if (tareaEjecutada.estado == EstadoTarea.Done) return;
            _notificationService.SendWindowsTaskNotificacionEndTime(tareaEjecutada);
        }

        public void UpdateTarea(Tarea updatedTarea, MainWindow? mainWindow = null)
        {
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
            }
        }

        public void DeleteTarea(Tarea tareaEliminada, MainWindow mainWindow)
        {
            _listaTareas.Remove(tareaEliminada);
            DeleteTimer(tareaEliminada);
            SaveTareas(_listaTareas);
            mainWindow.RecuperarTareasUI();
        }

        public void ChangeTaskStatus(Tarea tarea, EstadoTarea nuevoEstado, MainWindow? mainWindow = null)
        {
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
            }
        }

        public List<Tarea>? LoadTareas(MainWindow? mainWindow = null)
        {
            LoadTareasInternos();
            if (mainWindow is not null) mainWindow.RecuperarTareasUI();
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

            return Path.Combine(folder, "tasks.json");
        }

        private void LoadTareasInternos()
        {
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

            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists(LoadTareasPath()))
            {
                using (File.Create(LoadTareasPath())) { }
            }
            string conteindoJSON = File.ReadAllText(LoadTareasPath());
            if (string.IsNullOrEmpty(conteindoJSON))
            {
                _listaTareas = new List<Tarea>();
                return;
            }

            if (EncryptEnabled && RecoveryKey is not null && _configService?._configuracion.passwordEncrypt is not null)
            {
                conteindoJSON = Crypto.Decrypt(conteindoJSON, _configService._configuracion.passwordEncrypt);
            }


            List<Tarea>? tareasRecuperadas = JsonSerializer.Deserialize<List<Tarea>>(conteindoJSON);
            _listaTareas = tareasRecuperadas ?? new List<Tarea>();
        }

        public void SaveTareas(List<Tarea> tareas)
        {
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
                json = Crypto.Encrypt(json, _configService._configuracion.passwordEncrypt);
            }

            File.WriteAllText(LoadTareasPath(), json);
            _listaTareas = tareas;
        }

        public void RechargeTareasUI(List<Tarea>? tareasRecuperadas, MainWindow mainWindow)
        {
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
