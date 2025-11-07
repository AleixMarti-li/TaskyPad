using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
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
using System.Windows.Forms;
using System.Drawing;

namespace TaskyPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] _listaNotas = new string[0]; 
        private List<Tarea> _listaTareas = new List<Tarea>();
        public MainWindow()
        {
            InitializeComponent();
            AddVersionAppUI();
            RecuperarNotas();
            LoadTareas();
        }

        private void BtnCrearNotas_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists("notas")) {
                Directory.CreateDirectory("notas");
            }
            string nombreNota = Interaction.InputBox("Introduce el nombre de la nota");
            File.WriteAllText($"notas\\{nombreNota}.txt", @"{\rtf1\ansi }");
            RecuperarNotas();
        }

        public void RecuperarNotas() {
            string[] listaNotas = Directory.GetFiles("notas", "*.txt");
            _listaNotas = listaNotas;
            RecuperarNotasUI();
        }

        private void RecuperarNotasUI()
        {
            if (!Directory.Exists("notas")) Directory.CreateDirectory("notas");

            _listaNotas = Directory.GetFiles("notas", "*.txt");

            PanelNotas.Children.Clear();

            foreach (var item in _listaNotas)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(item);
                System.Windows.Controls.Button Btnpepe = new System.Windows.Controls.Button();
                Btnpepe.Content = fileName;
                Btnpepe.Margin = new Thickness(5);
                Btnpepe.Click += BtnNota_Click;
                PanelNotas.Children.Add(Btnpepe);
            }
        }

        private void BtnNota_Click(object sender, RoutedEventArgs e)
        {
            string nombre = ((System.Windows.Controls.ContentControl)sender).Content.ToString();
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
            _listaTareas.Add(nuevaTarea);
            SaveJSONTarea();
        }

        public void SaveJSONTarea() 
        {
            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists("tareas\\tasks.json"))
            {
                using (File.Create("tareas\\tasks.json")) { }
            }
            string json = JsonSerializer.Serialize(_listaTareas);
            File.WriteAllText("tareas\\tasks.json", json);
            LoadTareas();
        }

        public void LoadTareas() 
        {
            if (!Directory.Exists("tareas")) Directory.CreateDirectory("tareas");
            if (!File.Exists("tareas\\tasks.json"))
            {
                using (File.Create("tareas\\tasks.json")) { }
            }
            string conteindoJSON = File.ReadAllText("tareas\\tasks.json");
            if (string.IsNullOrEmpty(conteindoJSON)) return;
            List<Tarea>? tareasRecuperadas = JsonSerializer.Deserialize<List<Tarea>>(conteindoJSON);
            if (tareasRecuperadas is null) return;
            _listaTareas = tareasRecuperadas;
            RecuperarTareasUI();
        }

        private void RecuperarTareasUI()
        {
            PanelTareasNone.Children.Clear();
            PanelTareasInProgress.Children.Clear();
            PanelTareasDone.Children.Clear();

            foreach (var item in _listaTareas)
            {
                // Contenedor de la tarjeta de la tarea
                StackPanel card = new StackPanel();
                card.Margin = new Thickness(5);
                card.Orientation = System.Windows.Controls.Orientation.Vertical;
                card.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 235));
                //card.Padding = new Thickness(8);
                //card.CornerRadius = new CornerRadius(5);

                System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();
                System.Windows.Controls.MenuItem MenuItem = new System.Windows.Controls.MenuItem();
                MenuItem.Header = "Eliminar";
                MenuItem.Icon = "🗑️";
                MenuItem.Click += (s, e) => BorrarTarea(item);
                contextMenu.Items.Add(MenuItem);
                card.ContextMenu = contextMenu;

                // Título
                System.Windows.Controls.Label lblTitulo = new System.Windows.Controls.Label();
                lblTitulo.Content = item.titulo;
                lblTitulo.FontWeight = FontWeights.Bold;

                // Subtítulo
                System.Windows.Controls.Label lblSubtitulo = new System.Windows.Controls.Label();
                lblSubtitulo.Content = item.descripcion;

                // Tiempo
                System.Windows.Controls.Label lblTiempo = new System.Windows.Controls.Label();
                lblTiempo.Content = item.fecha.ToString("dd-MM-yyyy HH:mm");
                TimeSpan diferencia = item.fecha - DateTime.Now;
                if (diferencia.TotalHours <= 24 && diferencia.TotalHours > 0)
                {
                    lblTiempo.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                } 
                else if (diferencia.TotalHours <= 72 && diferencia.TotalHours > 0) 
                {
                    lblTiempo.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 0));
                }

                // ComboBox del estado
                System.Windows.Controls.ComboBox comboEstado = new System.Windows.Controls.ComboBox();
                comboEstado.Margin = new Thickness(0, 5, 0, 0);
                comboEstado.Width = 120;
                comboEstado.Items.Add("None");
                comboEstado.Items.Add("In Progress");
                comboEstado.Items.Add("Done");
                comboEstado.SelectedItem = item.estado.ToString();

                System.Windows.Controls.Button eliminarTarea = new System.Windows.Controls.Button();
                card.Margin = new Thickness(10,5,0,0);
                eliminarTarea.Width = 60;
                eliminarTarea.Content = "🗑️";
                eliminarTarea.Click += (s, e) => BorrarTarea(item);

                // Evento al cambiar estado
                comboEstado.SelectionChanged += (s, e) =>
                {
                    string seleccionado = comboEstado.SelectedItem.ToString();
                    if (Enum.TryParse<EstadoTarea>(seleccionado.Replace(" ", ""), out var nuevoEstado))
                    {
                        CambiarEstadoTarea(item, nuevoEstado);
                    }
                };

                // Añadimos todo al panel
                card.Children.Add(lblTitulo);
                card.Children.Add(lblSubtitulo);
                card.Children.Add(lblTiempo);
                card.Children.Add(comboEstado);

                // Lo colocamos según el estado actual
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
                        card.Children.Add(eliminarTarea);
                        break;
                }
            }
        }

        private void BorrarTarea(Tarea tareaEliminada) 
        {
            MessageBoxResult eliminarTarea = System.Windows.MessageBox.Show($"Estas seguro de eliminar la tarea de {tareaEliminada.titulo}? esta accion no se puede deshacer", "Advertencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (eliminarTarea != MessageBoxResult.Yes) return;
            _listaTareas.Remove(tareaEliminada);
            SaveJSONTarea();
        }

        private void CambiarEstadoTarea(Tarea item, EstadoTarea nuevoEstado)
        {
            item.estado = nuevoEstado;
            RecuperarTareasUI(); // refrescamos la UI
        }

        private void AddVersionAppUI() 
        {
            string getVersionAssembly = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            versionApp.Content = $"Version {getVersionAssembly}";
        }
    }
}