using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TaskyPad
{
    /// <summary>
    /// Lógica de interacción para ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        public MainWindow _ventanaPrincipal;
        public string _dataPath;
        public Config? _configuracion;
        public ConfigurationWindow(MainWindow ventanaPrincipal)
        {
            InitializeComponent();
            _ventanaPrincipal = ventanaPrincipal;
            _dataPath = LoadConfigPath();
            LoadConfigJSON();
            LoadConfigUI();
        }

        private void LoadConfigUI()
        {
            if (_configuracion is null) return;

            CheckStartOnWindowsStart.IsChecked = _configuracion.iniciarAuto;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _ventanaPrincipal.Show();
            base.OnClosing(e);
        }

        private string LoadConfigPath()
        {
            return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TaskyPad",
                "Configuration"
            );
        }

        public void LoadConfigJSON() 
        {
            if (!File.Exists($"{_dataPath}\\configuration.json"))
            {
                _configuracion = new Config();
                return;
            }

            string conteindoJSON = File.ReadAllText($"{_dataPath}\\configuration.json");

            if (string.IsNullOrEmpty(conteindoJSON)) return;

            Config? ConfigRecuperada = JsonSerializer.Deserialize<Config>(conteindoJSON);

            _configuracion = ConfigRecuperada ?? new Config();
        }

        public void SaveConfigJSON(Config guardarConfig) 
        {
            string PathFile = System.IO.Path.Combine(_dataPath, "configuration.json");

            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            if (!File.Exists(PathFile))
            {
                File.Create(PathFile).Dispose();
            }

            string json = JsonSerializer.Serialize(guardarConfig);
            File.WriteAllText(PathFile, json);
        }

        private void CheckStartOnWindowsStart_Click(object sender, RoutedEventArgs e)
        {
            if (_configuracion is null) return;
            bool? checkStart = CheckStartOnWindowsStart.IsChecked;
            if (checkStart.HasValue)
            {
                _configuracion.iniciarAuto = checkStart.Value;
            }

            SaveConfigJSON(_configuracion);
        }
    }
}
