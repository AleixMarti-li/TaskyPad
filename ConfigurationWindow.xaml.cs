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
        public Config _configuracion;
        public ConfigurationWindow(MainWindow ventanaPrincipal)
        {
            InitializeComponent();
            _ventanaPrincipal = ventanaPrincipal;
            LoadConfigJSON();

        }
        protected override void OnClosing(CancelEventArgs e)
        {
            _ventanaPrincipal.Show();
            base.OnClosing(e);
        }

        public void LoadConfigJSON() 
        {
            _dataPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TaskyPad",
                "Configuration"
            );

            if (!File.Exists($"{_dataPath}\\configuration.json")) return;

            string conteindoJSON = File.ReadAllText($"{_dataPath}\\configuration.json");

            if (string.IsNullOrEmpty(conteindoJSON)) return;

            Config? ConfigRecuperada = JsonSerializer.Deserialize<Config>(conteindoJSON);

            _configuracion = ConfigRecuperada ?? new Config();
        }

        public void SaveConfigJSON(Config guardarConfig) 
        { 
            
        }
    }
}
