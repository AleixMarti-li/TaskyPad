using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Velopack;

namespace TaskyPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] _listaNotas = new string[0]; 
        private NotifyIcon _TrayIcon;
        private UpdateManager updateManager;
        private returnMessageUpdateInfo _updateManagerResponse;
        private NotificationService notificationService;
        private TaskService? taskService;
        public MainWindow()
        {
            InitializeComponent();
            CreateNotificationService();
            CreateTaskService();
            loadTrayIcon();
            AddVersionAppUI();
            RecuperarNotas();
            LoadTareas();
            
            updateManager = new UpdateManager();
            CheckVersion();
        }
        private void CreateNotificationService()
        {
            notificationService = new NotificationService();
        }
        private void CreateTaskService()
        {
            taskService = new TaskService(notificationService);
            taskService.InicializeTimers();
        }
        private async void CheckVersion()
        {
            _updateManagerResponse = await updateManager.CheckActualizacionDisponible();

            if (!_updateManagerResponse.updateAvaliable) return;

            ButtonUpdateVersion.Visibility = Visibility.Visible;
            ButtonUpdateVersion.Content = $"Descargar actualización ({_updateManagerResponse.version})";
            //CustomMessageBoxResult eliminarNota = CustomMessageBox.Show(this, $"¿Seguro que quieras actualizar al a versión {_updateManagerResponse.version}?", $"Actualización {_updateManagerResponse.version}", CustomMessageBoxButton.YesNo, iconPath: "pack://application:,,,/Resources/warn.jpg", headerLogoPath: "pack://application:,,,/Resources/warn.jpg");
            //if (eliminarNota != CustomMessageBoxResult.Yes) return;
        }
        private async void ButtonUpdateVersion_Click(object sender, RoutedEventArgs e)
        {
            CustomMessageBoxResult eliminarNota = CustomMessageBox.Show(this, $"¿Seguro que quieras actualizar al a versión {_updateManagerResponse.version}?", $"Actualización {_updateManagerResponse.version}", CustomMessageBoxButton.YesNo, iconPath: "pack://application:,,,/Resources/warn.jpg", headerLogoPath: "pack://application:,,,/Resources/warn.jpg");
            if (eliminarNota != CustomMessageBoxResult.Yes) return;
            ButtonUpdateVersion.IsEnabled = false;
            ButtonUpdateVersion.Content = "Descargando actualización...";
            await updateManager.ForceUpdate();
            ButtonUpdateVersion.IsEnabled = true;
            ButtonUpdateVersion.Content = $"Descargar actualización ({_updateManagerResponse.version})";
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
#if !DEBUG
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
            _TrayIcon.Visible = true;
#endif
        }

        private void loadTrayIcon() 
        {
            _TrayIcon = new NotifyIcon();
            _TrayIcon.Visible = false;
            _TrayIcon.Text = "TaskyPad";
            _TrayIcon.Icon = new Icon("logo.ico");
            _TrayIcon.ContextMenuStrip = new ContextMenuStrip();
            _TrayIcon.ContextMenuStrip.Items.Add("Abrir", null, (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
            });
            _TrayIcon.ContextMenuStrip = new ContextMenuStrip();
            _TrayIcon.ContextMenuStrip.Items.Add("Salir", null, (s, e) =>
            {
                _TrayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });
            _TrayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
        }

        private void BtnCrearNotas_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists("notas")) {
                Directory.CreateDirectory("notas");
            }
            string nombreNota = Interaction.InputBox("Introduce el nombre de la nota");
            if (string.IsNullOrEmpty(nombreNota)) return;
            File.WriteAllText($"notas\\{nombreNota}.txt", @"{\rtf1\ansi }");
            RecuperarNotas();
        }

        public void RecuperarNotas() {
            if (!Directory.Exists("notas")) {
                Directory.CreateDirectory("notas");
            }
            string[] listaNotas = Directory.GetFiles("notas", "*.txt");
            _listaNotas = listaNotas;
            RecuperarNotasUI();
        }

        private void RecuperarNotasUI()
        {
            if (!Directory.Exists("notas")) Directory.CreateDirectory("notas");

            _listaNotas = Directory.GetFiles("notas", "*.txt");

            PanelNotas.Children.Clear();

            // Mostrar u ocultar el mensaje según si hay notas
            NoNotasMessage.Visibility = _listaNotas.Length == 0 ? Visibility.Visible : Visibility.Hidden;

            foreach (var item in _listaNotas)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(item);
                
                // Border para estilo mejorado
                Border notaBorder = new Border();
                notaBorder.CornerRadius = new CornerRadius(6);
                notaBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                notaBorder.Margin = new Thickness(0, 0, 0, 8);
                notaBorder.Padding = new Thickness(2);
                
                var shadowEffect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 3,
                    ShadowDepth = 1,
                    Opacity = 0.1,
                    Color = System.Windows.Media.Colors.Black
                };
                notaBorder.Effect = shadowEffect;

                System.Windows.Controls.Button btnNota = new System.Windows.Controls.Button();
                btnNota.Content = "📄 " + fileName;
                btnNota.Padding = new Thickness(10, 12, 10, 12);
                btnNota.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                btnNota.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                btnNota.BorderThickness = new Thickness(0);
                btnNota.FontSize = 12;
                btnNota.FontWeight = FontWeights.Medium;
                btnNota.Cursor = System.Windows.Input.Cursors.Hand;
                btnNota.Click += BtnNota_Click;

                notaBorder.Child = btnNota;
                PanelNotas.Children.Add(notaBorder);
            }
        }

        private void BtnNota_Click(object sender, RoutedEventArgs e)
        {
            string contenido = ((System.Windows.Controls.ContentControl)sender).Content.ToString();
            // Remover el emoji "📄 " del nombre
            string nombre = contenido.Replace("📄 ", "").Trim();
            EditorNota ventanaEditor = new EditorNota(nombre, this);
            ventanaEditor.Title = $"Editando la nota - {nombre}";
            ventanaEditor.ShowDialog();
        }

        private void CrearTarea_Click(object sender, RoutedEventArgs e)
        {
            CrearTarea ventanaCrearTarea = new CrearTarea(this);
            ventanaCrearTarea.Title = "Creando nueva tarea";
            ventanaCrearTarea.ShowDialog();
        }

        public void AddTareaToList(Tarea nuevaTarea) 
        {
            taskService?._listaTareas.Add(nuevaTarea);
            SaveJSONTarea();
        }

        public void UpdateTareaToList(Tarea updatedTarea)
        {
            taskService?.UpdateTarea(updatedTarea, this);
        }

        public void SaveJSONTarea() 
        {
            if (taskService is null) return;
            taskService.SaveTareas(taskService._listaTareas);
            LoadTareas();
        }

        public void LoadTareas() 
        {
            taskService?.LoadTareas(this);
        }

        public void RecuperarTareasUI()
        {
            if (taskService?._listaTareas is null) return;

            PanelTareasNone.Children.Clear();
            PanelTareasInProgress.Children.Clear();
            PanelTareasDone.Children.Clear();

            var tareasDivididas = new
            {
                None = taskService._listaTareas.Where(t => t.estado == EstadoTarea.None).ToList(),
                EnProgreso = taskService._listaTareas.Where(t => t.estado == EstadoTarea.InProgress).ToList(),
                Completada = taskService._listaTareas.Where(t => t.estado == EstadoTarea.Done).ToList()
            };

            NoTareasPorHacerMessage.Visibility = tareasDivididas.None.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            NoTareasEnProgresoMessage.Visibility = tareasDivididas.EnProgreso.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            NoTareasCompletadasMessage.Visibility = tareasDivididas.Completada.Count == 0 ? Visibility.Visible : Visibility.Hidden;

            foreach (var item in taskService._listaTareas)
            {
                // Contenedor de la tarjeta de la tarea con estilo mejorado
                Border cardBorder = new Border();
                cardBorder.CornerRadius = new CornerRadius(8);
                cardBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                cardBorder.BorderThickness = new Thickness(1);
                cardBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
                cardBorder.Margin = new Thickness(0, 0, 0, 10);
                cardBorder.Padding = new Thickness(12);

                // Efecto de sombra
                var shadowEffect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.15,
                    Color = System.Windows.Media.Colors.Black
                };
                cardBorder.Effect = shadowEffect;

                StackPanel card = new StackPanel();
                card.Orientation = System.Windows.Controls.Orientation.Vertical;

                // Context Menu
                System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();
                System.Windows.Controls.MenuItem MenuItemEdit = new System.Windows.Controls.MenuItem();
                MenuItemEdit.Header = "Editar";
                MenuItemEdit.Click += (s, e) => EditarTarea(item);
                contextMenu.Items.Add(MenuItemEdit);
                System.Windows.Controls.MenuItem MenuItem = new System.Windows.Controls.MenuItem();
                MenuItem.Header = "Eliminar";
                MenuItem.Click += (s, e) => BorrarTarea(item);
                contextMenu.Items.Add(MenuItem);
#if DEBUG
                System.Windows.Controls.MenuItem MenuItemDebugNotification = new System.Windows.Controls.MenuItem();
                MenuItemDebugNotification.Header = "Emular Notificación";
                MenuItemDebugNotification.Click += (s, e) => notificationService.SendWindowsTaskNotificacionEndTime(item);
                contextMenu.Items.Add(MenuItemDebugNotification);
#endif
                cardBorder.ContextMenu = contextMenu;

                // Título - Más grande y destacado
                TextBlock lblTitulo = new TextBlock();
                lblTitulo.Text = item.titulo;
                lblTitulo.FontWeight = FontWeights.Bold;
                lblTitulo.FontSize = 14;
                lblTitulo.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 62, 80));
                lblTitulo.Margin = new Thickness(0, 0, 0, 5);

                // Subtítulo - Descripción con estilo
                TextBlock lblSubtitulo = new TextBlock();
                lblSubtitulo.Text = item.descripcion;
                lblSubtitulo.FontSize = 12;
                lblSubtitulo.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141));
                lblSubtitulo.Margin = new Thickness(0, 0, 0, 8);
                lblSubtitulo.TextWrapping = TextWrapping.Wrap;

                // Tiempo - Con colores según urgencia
                Border timeBorder = new Border();
                timeBorder.CornerRadius = new CornerRadius(4);
                timeBorder.Padding = new Thickness(8, 6, 8, 6);
                timeBorder.Margin = new Thickness(0, 0, 0, 8);
                
                TextBlock lblTiempo = new TextBlock();
                lblTiempo.Text = item.fecha.ToString("dd-MM-yyyy HH:mm");
                lblTiempo.FontSize = 11;
                lblTiempo.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                
                TimeSpan diferencia = item.fecha - DateTime.Now;
                if (diferencia.TotalHours <= 24 && diferencia.TotalHours > 0)
                {
                    timeBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // Rojo urgente
                    lblTiempo.Text = "🔴 " + lblTiempo.Text + " - ¡URGENTE!";
                } 
                else if (diferencia.TotalHours <= 72 && diferencia.TotalHours > 0) 
                {
                    timeBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)); // Amarillo
                    lblTiempo.Text = "🟡 " + lblTiempo.Text;
                }
                else if (diferencia.TotalHours <= 0)
                {
                    timeBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 192, 192)); // Gris vencido
                    lblTiempo.Text = "⚠️ " + lblTiempo.Text + " - VENCIDA";
                }
                else
                {
                    timeBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)); // Azul
                }

                timeBorder.Child = lblTiempo;

                // ComboBox del estado con estilo mejorado
                System.Windows.Controls.ComboBox comboEstado = new System.Windows.Controls.ComboBox();
                comboEstado.Margin = new Thickness(0, 8, 0, 0);
                comboEstado.Padding = new Thickness(8, 6, 8, 6);
                comboEstado.FontSize = 11;
                comboEstado.Items.Add("None");
                comboEstado.Items.Add("In Progress");
                comboEstado.Items.Add("Done");
                comboEstado.SelectedItem = item.estado.ToString();

                // Evento al cambiar estado
                comboEstado.SelectionChanged += (s, e) =>
                {
                    string seleccionado = comboEstado.SelectedItem.ToString();
                    if (Enum.TryParse<EstadoTarea>(seleccionado.Replace(" ", ""), out var nuevoEstado))
                    {
                        CambiarEstadoTarea(item, nuevoEstado);
                    }
                };

                // Botón eliminar oculto hasta que la tarea esté completada
                System.Windows.Controls.Button eliminarTarea = new System.Windows.Controls.Button();
                eliminarTarea.Width = 100;
                eliminarTarea.Height = 32;
                eliminarTarea.Content = "🗑️ Eliminar";
                eliminarTarea.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                eliminarTarea.Foreground = new SolidColorBrush(System.Windows.Media.Colors.White);
                eliminarTarea.Margin = new Thickness(0, 8, 0, 0);
                eliminarTarea.FontSize = 11;
                eliminarTarea.Cursor = System.Windows.Input.Cursors.Hand;
                eliminarTarea.Click += (s, e) => BorrarTarea(item);

                // Añadimos todo al panel
                card.Children.Add(lblTitulo);
                card.Children.Add(lblSubtitulo);
                card.Children.Add(timeBorder);
                card.Children.Add(comboEstado);

                cardBorder.Child = card;

                // Lo colocamos según el estado actual
                switch (item.estado)
                {
                    case EstadoTarea.None:
                        PanelTareasNone.Children.Add(cardBorder);
                        break;
                    case EstadoTarea.InProgress:
                        PanelTareasInProgress.Children.Add(cardBorder);
                        break;
                    case EstadoTarea.Done:
                        card.Children.Add(eliminarTarea);
                        PanelTareasDone.Children.Add(cardBorder);
                        break;
                }
            }
        }

        private void EditarTarea(Tarea item)
        {
            CrearTarea editorCrearTarea = new CrearTarea(this, item);
            editorCrearTarea.Show();
        }

        private void BorrarTarea(Tarea tareaEliminada) 
        {
            MessageBoxResult eliminarTarea = System.Windows.MessageBox.Show($"Estas seguro de eliminar la tarea de {tareaEliminada.titulo}? esta accion no se puede deshacer", "Advertencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (eliminarTarea != MessageBoxResult.Yes) return;
            taskService?.DeleteTarea(tareaEliminada, this);
        }

        private void CambiarEstadoTarea(Tarea item, EstadoTarea nuevoEstado)
        {
            taskService?.ChangeTaskStatus(item, nuevoEstado, this);
        }

        private void AddVersionAppUI() 
        {
            Version? getVersionAssembly = Assembly.GetExecutingAssembly().GetName().Version;
            if (getVersionAssembly is null) return;
            versionApp.Content = $"v{getVersionAssembly.Major}.{getVersionAssembly.Minor}.{getVersionAssembly.Build}";
        }

        public void ExecuteTaskAction(string taskId, string action)
        {
            taskService?.ExecuteAction(taskId, action);
        }
    }
}