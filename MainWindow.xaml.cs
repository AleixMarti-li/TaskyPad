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
using Serilog;

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
        public TaskService taskService;
        private ConfigService configService;
        
        // Drag and drop fields
        private Tarea? _draggedTask = null;
        private System.Windows.Point _dragStartPoint;
        private bool _isDragging = false;
        private bool _isInitialized = false;

        public MainWindow()
        {
            Log.Information("==================================================");
            Log.Information("MainWindow constructor started");
            Log.Information("==================================================");
            
            try
            {
                Log.Information("Initializing WPF components");
                InitializeComponent();
                Log.Information("WPF components initialized successfully");

                Log.Information("Creating UpdateManager");
                updateManager = CreateUpdateManager();
                
                Log.Information("Creating ConfigService");
                configService = CreateConfigService();
                
                Log.Information("Creating NotificationService");
                notificationService = CreateNotificationService();
                
                // Initialize with a default value, will be loaded asynchronously later
                _updateManagerResponse = new returnMessageUpdateInfo(false);
                
                Log.Information("Creating TaskService");
                taskService = CreateTaskService();
                
                Log.Information("Loading TrayIcon");
                _TrayIcon = loadTrayIcon();
                
                Log.Information("Adding version to UI");
                AddVersionAppUI();
                
                Log.Information("Loading notes");
                RecuperarNotas();
                
                Log.Information("Loading tasks");
                LoadTareas();

                // Check if application started in silent mode
                bool isSilentMode = App.HasArgument("-silent");
                Log.Information("Application started in silent mode: {IsSilentMode}", isSilentMode);
                
                if (isSilentMode)
                {
                    Log.Information("Silent mode detected - showing TrayIcon");
                    _TrayIcon.Visible = true;
                    Log.Information("TrayIcon visibility set to: {Visible}", _TrayIcon.Visible);
                }
                
                // Log window state
                Log.Information("MainWindow initial state:");
                Log.Information("  - Visibility: {Visibility}", this.Visibility);
                Log.Information("  - WindowState: {WindowState}", this.WindowState);
                Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", this.ShowInTaskbar);
                Log.Information("  - IsVisible: {IsVisible}", this.IsVisible);
                
                Log.Information("MainWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical error in MainWindow constructor");
                throw;
            }
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            Log.Information("MainWindow OnContentRendered called");
            Log.Information("Window rendered state:");
            Log.Information("  - ActualWidth: {Width}", this.ActualWidth);
            Log.Information("  - ActualHeight: {Height}", this.ActualHeight);
            Log.Information("  - WindowState: {WindowState}", this.WindowState);
            Log.Information("  - IsLoaded: {IsLoaded}", this.IsLoaded);
            
            base.OnContentRendered(e);

            // Perform async initialization after the window is rendered
            if (!_isInitialized)
            {
                _isInitialized = true;
                await InitializeAsync();
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                Log.Information("Starting async initialization");
                
                Log.Information("Loading UpdateManager response");
                _updateManagerResponse = await LoadUpdateManagerResponse();
                Log.Information("UpdateManager response loaded successfully");
                
                Log.Information("Checking for updates");
                await CheckVersionAsync();
                Log.Information("Update check completed");
                
                Log.Information("Async initialization completed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during async initialization");
            }
        }
        
        protected override void OnActivated(EventArgs e)
        {
            Log.Information("MainWindow activated");
            Log.Information("  - WindowState: {WindowState}", this.WindowState);
            Log.Information("  - Topmost: {Topmost}", this.Topmost);
            
            base.OnActivated(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            Log.Information("MainWindow deactivated");
            base.OnDeactivated(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            Log.Information("MainWindow state changed to: {WindowState}", this.WindowState);
            
            if (this.WindowState == WindowState.Minimized)
            {
                Log.Information("Window minimized - considering hiding to tray");
            }
            else if (this.WindowState == WindowState.Normal)
            {
                Log.Information("Window restored to normal state");
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                Log.Information("Window maximized");
            }
            
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Log.Information("MainWindow OnClosing event triggered");
            Log.Information("  - Build configuration: {BuildConfig}", 
#if DEBUG
                "DEBUG"
#else
                "RELEASE"
#endif
            );
            
#if !DEBUG
            Log.Information("Release mode - cancelling close and hiding to tray");
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
            _TrayIcon.Visible = true;
            
            Log.Information("Window hidden - new state:");
            Log.Information("  - Visibility: {Visibility}", this.Visibility);
            Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", this.ShowInTaskbar);
            Log.Information("  - TrayIcon.Visible: {TrayIconVisible}", _TrayIcon?.Visible);
#else
            Log.Information("Debug mode - allowing window to close");
#endif
        }

        protected override void OnClosed(EventArgs e)
        {
            Log.Information("MainWindow OnClosed event - window fully closed");
            base.OnClosed(e);
        }

        private Forms.NotifyIcon loadTrayIcon() 
        {
            Log.Information("Creating TrayIcon");
            
            Forms.NotifyIcon _TrayIcon = new Forms.NotifyIcon();
            _TrayIcon.Visible = false;
            _TrayIcon.Text = "TaskyPad";
            
            try
            {
                _TrayIcon.Icon = new Icon("Resources\\logo.ico");
                Log.Information("TrayIcon icon loaded successfully from: Resources\\logo.ico");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load TrayIcon icon");
            }
            
            _TrayIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            
            // Add "Open" menu item
            _TrayIcon.ContextMenuStrip.Items.Add("Abrir", null, (s, e) =>
            {
                Log.Information("TrayIcon 'Abrir' clicked - restoring window");
                Log.Information("Current window state before restore:");
                Log.Information("  - Visibility: {Visibility}", this.Visibility);
                Log.Information("  - WindowState: {WindowState}", this.WindowState);
                Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", this.ShowInTaskbar);
                
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
                
                Log.Information("Window restored - new state:");
                Log.Information("  - Visibility: {Visibility}", this.Visibility);
                Log.Information("  - WindowState: {WindowState}", this.WindowState);
                Log.Information("  - ShowInTaskbar: {ShowInTaskbar}", this.ShowInTaskbar);
                Log.Information("  - IsActive: {IsActive}", this.IsActive);
            });
            
            // Add "Exit" menu item
            _TrayIcon.ContextMenuStrip.Items.Add("Salir", null, (s, e) =>
            {
                Log.Information("TrayIcon 'Salir' clicked - shutting down application");
                _TrayIcon.Visible = false;
                Log.Information("TrayIcon hidden, initiating application shutdown");
                System.Windows.Application.Current.Shutdown();
            });
            
            // Double-click handler
            _TrayIcon.DoubleClick += (s, e) =>
            {
                Log.Information("TrayIcon double-clicked - restoring window");
                Log.Information("Current window state before restore:");
                Log.Information("  - Visibility: {Visibility}", this.Visibility);
                Log.Information("  - WindowState: {WindowState}", this.WindowState);
                
                this.Show();
                this.WindowState = WindowState.Normal;
                
                Log.Information("Window restored via double-click - new state:");
                Log.Information("  - Visibility: {Visibility}", this.Visibility);
                Log.Information("  - WindowState: {WindowState}", this.WindowState);
            };

            Log.Information("TrayIcon created successfully");
            Log.Information("  - Initial Visibility: {Visible}", _TrayIcon.Visible);
            Log.Information("  - Text: {Text}", _TrayIcon.Text);
            
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

            // Find ScrollViewers and their parent Borders dynamically
            ScrollViewer? scrollViewerNone = this.FindName("ScrollViewerTareasNone") as ScrollViewer;
            ScrollViewer? scrollViewerInProgress = this.FindName("ScrollViewerTareasInProgress") as ScrollViewer;
            ScrollViewer? scrollViewerDone = this.FindName("ScrollViewerTareasDone") as ScrollViewer;

            // Setup drop targets on both ScrollViewer and StackPanel
            // Primary target is ScrollViewer if it exists, fallback to StackPanel
            UIElement targetNone = (UIElement?)scrollViewerNone ?? PanelTareasNone;
            UIElement targetInProgress = (UIElement?)scrollViewerInProgress ?? PanelTareasInProgress;
            UIElement targetDone = (UIElement?)scrollViewerDone ?? PanelTareasDone;

            SetupDropTarget(targetNone, PanelTareasNone, EstadoTarea.None);
            SetupDropTarget(targetInProgress, PanelTareasInProgress, EstadoTarea.InProgress);
            SetupDropTarget(targetDone, PanelTareasDone, EstadoTarea.Done);

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

            // Drag and drop events
            cardBorder.MouseLeftButtonDown += (s, e) =>
            {
                _dragStartPoint = e.GetPosition(null);
                _draggedTask = item;
            };

            cardBorder.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && _draggedTask == item)
                {
                    System.Windows.Point currentPosition = e.GetPosition(null);
                    System.Windows.Vector diff = _dragStartPoint - currentPosition;

                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        _isDragging = true;
                        System.Windows.DataObject dragData = new System.Windows.DataObject(System.Windows.DataFormats.Serializable, item);
                        System.Windows.DragDrop.DoDragDrop(cardBorder, dragData, System.Windows.DragDropEffects.Move);
                    }
                }
            };

            cardBorder.MouseLeftButtonUp += (s, e) =>
            {
                _isDragging = false;
                _draggedTask = null;
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

        private Button CreateActionButton(String content, System.Windows.Media.Color bgColor)
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

        // Drag and Drop helper methods - Improved version
        private void SetupDropTarget(UIElement scrollViewer, StackPanel panel, EstadoTarea targetStatus)
        {
            // Enable drop on both the ScrollViewer and the StackPanel
            scrollViewer.AllowDrop = true;
            panel.AllowDrop = true;

            // Handler para eventos de drop
            System.Windows.DragEventHandler dragEnterHandler = (s, e) =>
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.Serializable))
                {
                    e.Effects = System.Windows.DragDropEffects.Move;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
                e.Handled = true;
            };

            System.Windows.DragEventHandler dragOverHandler = (s, e) =>
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.Serializable))
                {
                    e.Effects = System.Windows.DragDropEffects.Move;
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
                e.Handled = true;
            };

            System.Windows.DragEventHandler dropHandler = (s, e) =>
            {
                HandleDrop(e, targetStatus);
                e.Handled = true;
            };

            // ScrollViewer events
            scrollViewer.DragEnter += dragEnterHandler;
            scrollViewer.DragOver += dragOverHandler;
            scrollViewer.Drop += dropHandler;

            // StackPanel events
            panel.DragEnter += dragEnterHandler;
            panel.DragOver += dragOverHandler;
            panel.Drop += dropHandler;
        }

        private void HandleDrop(System.Windows.DragEventArgs e, EstadoTarea targetStatus)
        {
            if (e.Data.GetData(System.Windows.DataFormats.Serializable) is Tarea draggedTask)
            {
                if (draggedTask.estado != targetStatus)
                {
                    CambiarEstadoTarea(draggedTask, targetStatus);
                }
            }
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

        private async Task CheckVersionAsync()
        {
            _updateManagerResponse = await updateManager.CheckActualizacionDisponible();

            if (!_updateManagerResponse.updateAvaliable) return;

            ButtonUpdateVersion.Visibility = Visibility.Visible;
            ButtonUpdateVersion.Content = $"Descargar actualización ({_updateManagerResponse.version})";
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
    }
}