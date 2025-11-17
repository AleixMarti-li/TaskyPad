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
        public ConfigService _configService;
        public ConfigurationWindow(MainWindow ventanaPrincipal, ConfigService configService)
        {
            InitializeComponent();
            _ventanaPrincipal = ventanaPrincipal;
            _configService = configService;
            LoadConfigUI();
        }

        private void LoadConfigUI()
        {
            if (_configService._configuracion is null) return;

            CheckStartOnWindowsStart.IsChecked = _configService._configuracion.iniciarAuto;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _ventanaPrincipal.Show();
            base.OnClosing(e);
        }

        private void CheckStartOnWindowsStart_Click(object sender, RoutedEventArgs e)
        {
            if (_configService._configuracion is null) return;
            bool? checkStart = CheckStartOnWindowsStart.IsChecked;
            if (checkStart.HasValue)
            {
                _configService._configuracion.iniciarAuto = checkStart.Value;
            }

            _configService.SaveConfigJSON();
        }
    }
}
