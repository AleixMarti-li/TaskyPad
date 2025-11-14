using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace TaskyPad
{
    public class TaskService
    {
        public List<Tarea> _listaTareas = new List<Tarea>();
        private Dictionary<string, System.Timers.Timer> tasksDictionary = new Dictionary<string, System.Timers.Timer>();
        private NotificationService _notificationService;
        public TaskService(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public TaskService()
        {
            _notificationService = new NotificationService();
        }

        public void ExecuteAction(string taskId, string action)
        {
            Tarea? TareaSeleccionada = _listaTareas.FirstOrDefault(t => t.idTarea == taskId);
            if (TareaSeleccionada is null) return;

            ChangeTaskStatus(TareaSeleccionada, EstadoTarea.Done);
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

        public void UpdateTarea(Tarea updatedTarea, MainWindow mainWindow)
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
                mainWindow.RecuperarTareasUI();
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

        private void LoadTareasInternos()
        {
            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists("tareas\\tasks.json"))
            {
                using (File.Create("tareas\\tasks.json")) { }
            }
            string conteindoJSON = File.ReadAllText("tareas\\tasks.json");
            if (string.IsNullOrEmpty(conteindoJSON))
            {
                _listaTareas = new List<Tarea>();
                return;
            }
            List<Tarea>? tareasRecuperadas = JsonSerializer.Deserialize<List<Tarea>>(conteindoJSON);
            _listaTareas = tareasRecuperadas ?? new List<Tarea>();
        }

        public void SaveTareas(List<Tarea> tareas)
        {
            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists("tareas\\tasks.json"))
            {
                using (File.Create("tareas\\tasks.json")) { }
            }
            string json = JsonSerializer.Serialize(tareas);
            File.WriteAllText("tareas\\tasks.json", json);
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
