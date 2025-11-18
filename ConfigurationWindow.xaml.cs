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
using TaskyPad.Services;

namespace TaskyPad
{
    /// <summary>
    /// Lógica de interacción para ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        public MainWindow _ventanaPrincipal;
        public ConfigService _configService;
        private AutoStartService _autoStartService;

        public ConfigurationWindow(MainWindow ventanaPrincipal, ConfigService configService)
        {
            InitializeComponent();
            _ventanaPrincipal = ventanaPrincipal;
            _configService = configService;
            _autoStartService = new AutoStartService();
            LoadConfigUI();
        }

        private void LoadConfigUI()
        {
            if (_configService._configuracion is null) return;

            CheckStartOnWindowsStart.IsChecked = _configService._configuracion.iniciarAuto;
            CheckEnableEncrypt.IsChecked = _configService._configuracion.enableEncrypt;
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
                
                // Usar AutoStartService para gestionar el registro de Windows
                if (checkStart.Value)
                {
                    bool success = _autoStartService.EnableAutoStart();
                    if (!success)
                    {
                        CustomMessageBox.ShowOkDialog(this, "No se pudo habilitar el inicio automático. Verifica los permisos.", "Error");
                        CheckStartOnWindowsStart.IsChecked = false;
                        _configService._configuracion.iniciarAuto = false;
                        return;
                    }
                }
                else
                {
                    bool success = _autoStartService.DisableAutoStart();
                    if (!success)
                    {
                        CustomMessageBox.ShowOkDialog(this, "No se pudo desactivar el inicio automático.", "Error");
                        CheckStartOnWindowsStart.IsChecked = true;
                        _configService._configuracion.iniciarAuto = true;
                        return;
                    }
                }
            }

            _configService.SaveConfigJSON();
        }

        private void CheckEnableEncrypt_Click(object sender, RoutedEventArgs e)
        {
            if (_configService._configuracion is null) return;
            bool? checkEnableEncrypt = CheckEnableEncrypt.IsChecked;
            if (checkEnableEncrypt.HasValue)
            {
                if (checkEnableEncrypt.Value)
                {
                    CustomMessageBoxResult resultadoConfirmacion = CustomMessageBox.ShowConfirmation(this, "¿Estás seguro de activar el cifrado? Asegúrate de guardar una contraseña segura, ya que será necesaria para acceder a tus datos.", "Confirmar activar cifrado", CustomMessageBoxButton.OKCancel);
                    if (resultadoConfirmacion is not CustomMessageBoxResult.OK) 
                    {
                        CheckEnableEncrypt.IsChecked = false;
                        return;
                    }

                    string? customMessageBoxResultOldPassword = CustomMessageBox.ShowInput(this, "Por favor, ingresa la contraseña de encriptaje.", "Confirmar contraseña", "", "Contraseña");
                    string? customMessageBoxResultOldPasswordConfirm = CustomMessageBox.ShowInput(this, "Por favor, ingresa nuevamente la contraseña de encriptaje para confirmarla.", "Confirmar contraseña", "", "Contraseña");

                    if (customMessageBoxResultOldPassword is null || customMessageBoxResultOldPasswordConfirm is null || customMessageBoxResultOldPassword != customMessageBoxResultOldPasswordConfirm)
                    {
                        CustomMessageBox.ShowOkDialog(this, "Las contraseñas no coinciden. El cifrado no ha sido activado.", "Contraseñas no coinciden");
                        CheckEnableEncrypt.IsChecked = false;
                        return;
                    }

                    MigrateDataToDecrypt(out List<Tarea>? listTareas);

                    _configService._configuracion.enableEncrypt = true;
                    _configService._configuracion.passwordEncrypt = customMessageBoxResultOldPassword;
                    _configService.SaveConfigJSON();

                    MigrateDataToEncrypt(listTareas);
                }
                else 
                {
                    if (_configService._configuracion.enableEncrypt)
                    {
                        CustomMessageBoxResult resultadoConfirmacion = CustomMessageBox.ShowConfirmation(this, "¿Estás seguro de desactivar el cifrado? Tus datos serán desencriptados y la contraseña guardada será eliminada.", "Confirmar desactivar cifrado", CustomMessageBoxButton.OKCancel);

                        if (resultadoConfirmacion is not CustomMessageBoxResult.OK) return;

                        if (_configService._configuracion.passwordEncrypt is not null && _configService._configuracion.passwordEncrypt != "")
                        {
                            string? customMessageBoxResultOldPassword = CustomMessageBox.ShowInput(this, "Por favor, ingresa la contraseña actual para confirmarla.", "Confirmar contraseña", "", "Contraseña");
                            if (customMessageBoxResultOldPassword is null || customMessageBoxResultOldPassword != _configService._configuracion.passwordEncrypt)
                            {
                                CustomMessageBox.ShowOkDialog(this, "La contraseña no es correcta. El cifrado no ha sido desactivado.", "Contraseña incorrecta");
                                CheckEnableEncrypt.IsChecked = true;
                                return;
                            }
                        }

                        MigrateDataToDecrypt(out List<Tarea>? listTareas);
                        _configService._configuracion.enableEncrypt = false;
                        _configService._configuracion.passwordEncrypt = null;
                        _configService.SaveConfigJSON();
                        MigrateDataToEncrypt(listTareas);
                    }
                }
            }
        }

        private void BtnGuardarContra_Click(object sender, RoutedEventArgs e)
        {
            CustomMessageBoxResult resultadoConfirmacion = CustomMessageBox.ShowConfirmation(this, "¿Estás seguro de guardar esta contraseña? Asegúrate de recordarla, ya que será necesaria para desencriptar tus datos.", "Confirmar guardar contraseña", CustomMessageBoxButton.OKCancel);
            
            if (_configService._configuracion.passwordEncrypt is not null && _configService._configuracion.passwordEncrypt != "")
            {
                string? customMessageBoxResultOldPassword = CustomMessageBox.ShowInput(this, "Por favor, ingresa la contraseña antigua para confirmarla.", "Confirmar contraseña", "", "Contraseña");
                if (customMessageBoxResultOldPassword is null || customMessageBoxResultOldPassword != _configService._configuracion.passwordEncrypt)
                {
                    CustomMessageBox.ShowOkDialog(this, "La contraseña antigua no es correcta. La nueva contraseña no ha sido guardada.", "Contraseña incorrecta");
                    return;
                }
            }

            string? contrasena = CustomMessageBox.ShowInput(this, "Por favor, ingresa la nueva contraseña de encriptaje.", "Nueva contraseña", "", "Contraseña");
            string? contrasenaConfirm = CustomMessageBox.ShowInput(this, "Por favor, ingresa nuevamente la nueva contraseña de encriptaje para confirmarla.", "Confirmar nueva contraseña", "", "Contraseña");

            if (contrasena is null || contrasenaConfirm is null || contrasena != contrasenaConfirm)
            {
                CustomMessageBox.ShowOkDialog(this, "Las contraseñas no coinciden. La nueva contraseña no ha sido guardada.", "Contraseñas no coinciden");
                return;
            }

            MigrateDataToDecrypt(out List<Tarea>? listTareas);

            _configService._configuracion.passwordEncrypt = contrasena;
            _configService.SaveConfigJSON();

            MigrateDataToEncrypt(listTareas);
        }

        private void MigrateDataToDecrypt(out List<Tarea>? listTareas)
        {
            listTareas = _ventanaPrincipal.taskService.LoadTareas(_ventanaPrincipal);
        }

        private void MigrateDataToEncrypt(List<Tarea>? listTareas)
        {
            if (listTareas is null) return;
            _ventanaPrincipal.taskService.SaveTareas(listTareas);
        }
    }
}
