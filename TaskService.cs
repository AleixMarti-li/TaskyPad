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
            List<Tarea>? LoadTareasList = LoadTareas();
            if (LoadTareasList is null) return;
            Tarea? TareaSeleccionada = LoadTareasList.FirstOrDefault(t => t.idTarea == taskId);
            if (TareaSeleccionada is null) return;

        }

        public void InicializeTimers()
        {
            List<Tarea>? listaTareasParaTimers = LoadTareas();
            if (listaTareasParaTimers is null) return;
            foreach (Tarea teareaIndividual in listaTareasParaTimers)
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
            _notificationService.SendWindowsTaskNotificacionEndTime(tareaEjecutada);
        }

        public List<Tarea>? LoadTareas(MainWindow? mainWindow = null)
        {
            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists("tareas\\tasks.json"))
            {
                using (File.Create("tareas\\tasks.json")) { }
            }
            string conteindoJSON = File.ReadAllText("tareas\\tasks.json");
            if (string.IsNullOrEmpty(conteindoJSON)) return new List<Tarea>();
            List<Tarea>? tareasRecuperadas = JsonSerializer.Deserialize<List<Tarea>>(conteindoJSON);

            if (mainWindow is not null) RechargeTareasUI(tareasRecuperadas, mainWindow);

            return tareasRecuperadas;
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
            mainWindow._listaTareas = tareasRecuperadas;
            mainWindow.RecuperarTareasUI();
        }
    }
}
