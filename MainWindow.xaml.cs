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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Velopack;
using System.Windows.Controls.Primitives;
using Forms = System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using Orientation = System.Windows.Controls.Orientation;

namespace TaskyPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] _listaNotas = new string[0]; 
        private Forms.NotifyIcon? _TrayIcon;
        private UpdateManager updateManager;
        private returnMessageUpdateInfo _updateManagerResponse;
        private NotificationService notificationService;
        private TaskService taskService;
        private ConfigService configService;
        public MainWindow()
        {
            InitializeComponent();
            updateManager = CreateUpdateManager();
            configService = CreateConfigService();
            notificationService = CreateNotificationService();
            _updateManagerResponse = LoadUpdateManagerResponse().GetAwaiter().GetResult();
            taskService = CreateTaskService();
            _TrayIcon = loadTrayIcon();
            AddVersionAppUI();
            RecuperarNotas();
            LoadTareas();

            CheckVersion();
        }
        private NotificationService CreateNotificationService()
        {
            return new NotificationService();
        }
        private ConfigService CreateConfigService()
        {
            return new ConfigService();
        }

        private UpdateManager CreateUpdateManager()
        {
            return new UpdateManager();
        }

        private TaskService CreateTaskService()
        {
            TaskService _taskService = new TaskService(this, notificationService, configService);
            _taskService.InicializeTimers();
            return _taskService;
        }

        private async Task<returnMessageUpdateInfo> LoadUpdateManagerResponse()
        {
            return await updateManager.CheckActualizacionDisponible();
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
            CustomMessageBoxResult eliminarNota = CustomMessageBox.ShowConfirmation(this, $"¿Estás completamente seguro de que deseas actualizar a la versión v({_updateManagerResponse.version})? \n\nEsta acción reemplazará la versión que estás usando ahora mismo y aplicará todos los cambios incluidos en la nueva actualización.", $"Actualizar a la versión v{_updateManagerResponse.version}", CustomMessageBoxButton.YesNo, iconPath: "pack://application:,,,/Resources/cloudUpdate.png", headerLogoPath: "pack://application:,,,/Resources/logo.ico");
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

        private Forms.NotifyIcon loadTrayIcon() 
        {
            Forms.NotifyIcon _TrayIcon = new Forms.NotifyIcon();
            _TrayIcon.Visible = false;
            _TrayIcon.Text = "TaskyPad";
            _TrayIcon.Icon = new Icon("Resources\\logo.ico");
            _TrayIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            _TrayIcon.ContextMenuStrip.Items.Add("Abrir", null, (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
            });
            _TrayIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
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

            return _TrayIcon;
        }

        private void BtnCrearNotas_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists("notas")) {
                Directory.CreateDirectory("notas");
            }

            string? nombreNota = CustomMessageBox.ShowInput(this, "Introduce el nombre de la nota", "Ingreso de datos", headerLogoPath: "pack://application:,,,/Resources/logo.ico");
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
            string? contenido = ((System.Windows.Controls.ContentControl)sender).Content.ToString();
            // Remover el emoji "📄 " del nombre
            if (contenido is null) return;
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

            // Update counters
            CountTareasNone.Text = $"{tareasDivididas.None.Count} tareas";
            CountTareasInProgress.Text = $"{tareasDivididas.EnProgreso.Count} tareas";
            CountTareasDone.Text = $"{tareasDivididas.Completada.Count} tareas";

            NoTareasPorHacerMessage.Visibility = tareasDivididas.None.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            NoTareasEnProgresoMessage.Visibility = tareasDivididas.EnProgreso.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            NoTareasCompletadasMessage.Visibility = tareasDivididas.Completada.Count == 0 ? Visibility.Visible : Visibility.Hidden;

            foreach (var item in taskService._listaTareas)
            {
                Border card = CreateTaskCard(item);

                // Add to appropriate panel
                switch (item.estado)
                {
                    case EstadoTarea.None:
                        PanelTareasNone.Children.Add(card);
                        break;
                    case EstadoTarea.InProgress:
                        PanelTareasInProgress.Children.Add(card);
                        break;
                    case EstadoTarea.Done:
                        PanelTareasDone.Children.Add(card);
                        break;
                }
            }
        }

        private Border CreateTaskCard(Tarea item)
        {
            // Card container
            Border cardBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)),
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.15,
                    Color = System.Windows.Media.Colors.Black
                }
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Priority indicator bar
            Border priorityBar = new Border
            {
                Background = GetPriorityColor(item),
                Height = 3
            };
            Grid.SetRow(priorityBar, 0);
            mainGrid.Children.Add(priorityBar);

            // Main content
            StackPanel contentPanel = new StackPanel
            {
                Margin = new Thickness(14, 12, 14, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(contentPanel, 1);

            // Title
            TextBlock titleBlock = new TextBlock
            {
                Text = item.titulo,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80)),
                Margin = new Thickness(0, 0, 0, 6),
                TextWrapping = TextWrapping.Wrap
            };
            contentPanel.Children.Add(titleBlock);

            // Description
            TextBlock descBlock = new TextBlock
            {
                Text = item.descripcion,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis,
                MaxHeight = 50
            };
            contentPanel.Children.Add(descBlock);

            // Due date badge
            TimeSpan diferencia = item.fecha - DateTime.Now;
            (Border dateBadge, string dateText) = GetDateBadge(item.fecha, diferencia);
            dateBadge.Margin = new Thickness(0, 0, 0, 10);
            contentPanel.Children.Add(dateBadge);

            // Status label
            TextBlock statusLabel = new TextBlock
            {
                Text = "Estado",
                FontSize = 10,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166)),
                Margin = new Thickness(0, 0, 0, 4),
                FontWeight = FontWeights.SemiBold
            };
            contentPanel.Children.Add(statusLabel);

            // Status ComboBox
            ComboBox statusCombo = new ComboBox
            {
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80))
            };
            statusCombo.Items.Add("📋 Por hacer");
            statusCombo.Items.Add("⏳ En progreso");
            statusCombo.Items.Add("✓ Completada");
            statusCombo.SelectedIndex = (int)item.estado;

            statusCombo.SelectionChanged += (s, e) =>
            {
                if (Enum.TryParse<EstadoTarea>(statusCombo.SelectedIndex.ToString(), out var nuevoEstado))
                {
                    CambiarEstadoTarea(item, nuevoEstado);
                }
            };
            contentPanel.Children.Add(statusCombo);

            mainGrid.Children.Add(contentPanel);

            // Delete button panel (only for completed tasks)
            if (item.estado == EstadoTarea.Done)
            {
                StackPanel actionPanel = new StackPanel
                {
                    Margin = new Thickness(14, 12, 14, 12),
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };
                Grid.SetRow(actionPanel, 2);

                Button deleteBtn = CreateActionButton("🗑️ Eliminar", System.Windows.Media.Color.FromRgb(231, 76, 60));
                deleteBtn.Click += (s, e) => BorrarTarea(item);
                deleteBtn.Margin = new Thickness(0, 0, 8, 0);
                actionPanel.Children.Add(deleteBtn);

                Button restoreBtn = CreateActionButton("↩️ Reabrir", System.Windows.Media.Color.FromRgb(52, 152, 219));
                restoreBtn.Click += (s, e) => ReiniciarTarea(item);
                actionPanel.Children.Add(restoreBtn);

                mainGrid.Children.Add(actionPanel);
            }

            // Context menu
            ContextMenu contextMenu = new ContextMenu();
            MenuItem editItem = new MenuItem { Header = "✏️ Editar" };
            editItem.Click += (s, e) => EditarTarea(item);
            contextMenu.Items.Add(editItem);

            MenuItem deleteItem = new MenuItem { Header = "🗑️ Eliminar" };
            deleteItem.Click += (s, e) => BorrarTarea(item);
            contextMenu.Items.Add(deleteItem);

            contextMenu.Items.Add(new Separator());

            MenuItem priorityItem = new MenuItem { Header = "📌 Marcar como prioritaria" };
            priorityItem.Click += (s, e) => TogglePriority(item);
            contextMenu.Items.Add(priorityItem);

            cardBorder.ContextMenu = contextMenu;

            cardBorder.Child = mainGrid;
            return cardBorder;
        }

        private SolidColorBrush GetPriorityColor(Tarea item)
        {
            return new SolidColorBrush(item.esPrioritaria ? 
                System.Windows.Media.Color.FromRgb(231, 76, 60) :  // Red for high priority
                System.Windows.Media.Color.FromRgb(52, 152, 219)); // Blue for normal
        }

        private (Border, string) GetDateBadge(DateTime fecha, TimeSpan diferencia)
        {
            Border badge = new Border
            {
                Padding = new Thickness(8, 5, 8, 5),
                CornerRadius = new CornerRadius(4)
            };

            string dateText;
            if (diferencia.TotalHours <= 0)
            {
                badge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
                dateText = $"⚠️ {fecha:dd-MM-yyyy HH:mm} - VENCIDA";
            }
            else if (diferencia.TotalHours <= 24)
            {
                badge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                dateText = $"🔴 {fecha:dd-MM-yyyy HH:mm} - ¡URGENTE!";
            }
            else if (diferencia.TotalHours <= 72)
            {
                badge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
                dateText = $"🟡 {fecha:dd-MM-yyyy HH:mm}";
            }
            else
            {
                badge.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                dateText = $"📅 {fecha:dd-MM-yyyy HH:mm}";
            }

            TextBlock dateBlock = new TextBlock
            {
                Text = dateText,
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                FontWeight = FontWeights.SemiBold
            };

            badge.Child = dateBlock;
            return (badge, dateText);
        }

        private Button CreateActionButton(string content, System.Windows.Media.Color bgColor)
        {
            return new Button
            {
                Content = content,
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 11,
                Background = new SolidColorBrush(bgColor),
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontWeight = FontWeights.SemiBold
            };
        }

        private void TogglePriority(Tarea item)
        {
            item.esPrioritaria = !item.esPrioritaria;
            SaveJSONTarea();
        }

        private void ReiniciarTarea(Tarea item)
        {
            CambiarEstadoTarea(item, EstadoTarea.None);
        }

        private void EditarTarea(Tarea item)
        {
            CrearTarea editorCrearTarea = new CrearTarea(this, item);
            editorCrearTarea.Show();
        }

        private void BorrarTarea(Tarea tareaEliminada) 
        {
            CustomMessageBoxResult eliminarNota = CustomMessageBox.ShowConfirmation(this, "¿Quieres eliminar esta tarea?", "Confirma que deseas proceder con la eliminación.", CustomMessageBoxButton.YesNo, iconPath: "pack://application:,,,/Resources/trash.png", headerLogoPath: "pack://application:,,,/Resources/logo.ico");
            if (eliminarNota != CustomMessageBoxResult.Yes) return;
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
            versionApp.Text = $"v{getVersionAssembly.Major}.{getVersionAssembly.Minor}.{getVersionAssembly.Build}";
        }

        public void ExecuteTaskAction(String taskId, String action) {
            taskService?.ExecuteAction(taskId, action);
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            if (configService is null) return;
            ConfigurationWindow ventanaConfigruacion = new ConfigurationWindow(this, configService);
            ventanaConfigruacion.Title = "Configuración";
            this.Hide();
            ventanaConfigruacion.ShowDialog();
        }
    }
}