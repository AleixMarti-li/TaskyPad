using System;
using System.Windows;

namespace TaskyPad
{
    public enum CustomMessageBoxResult { None, OK, Cancel, Yes, No }
    public enum CustomMessageBoxButton { OK, OKCancel, YesNo, YesNoCancel }

    public partial class CustomMessageBox : Window
    {
        private CustomMessageBoxResult _result = CustomMessageBoxResult.None;

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        // Método estático para mostrar el diálogo (síncrono, devuelve resultado)
        public static CustomMessageBoxResult ShowConfirmation(Window owner, string message, string title = "",
            CustomMessageBoxButton buttons = CustomMessageBoxButton.OK, string iconPath = null, string headerLogoPath = null)
        {
            var dlg = new CustomMessageBox();
            if (owner != null) dlg.Owner = owner;

            dlg.TxtMessage.Text = message ?? "";
            dlg.TxtTitle.Text = title ?? "";

            // Icono principal (opcional)
            if (!string.IsNullOrEmpty(iconPath))
            {
                dlg.IconImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute));
                dlg.IconImage.Visibility = Visibility.Visible;
            }

            // Logo en el header (opcional)
            if (!string.IsNullOrEmpty(headerLogoPath))
            {
                dlg.LogoHeader.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(headerLogoPath, UriKind.RelativeOrAbsolute));
                dlg.LogoHeader.Visibility = Visibility.Visible;
            }

            // Configurar botones según enum
            switch (buttons)
            {
                case CustomMessageBoxButton.OK:
                    dlg.ShowPrimary("OK", CustomMessageBoxResult.OK, isAccept: true);
                    break;
                case CustomMessageBoxButton.OKCancel:
                    dlg.ShowSecondary("Cancelar", CustomMessageBoxResult.Cancel, isCancel: true);
                    dlg.ShowPrimary("OK", CustomMessageBoxResult.OK, isAccept: true);
                    break;
                case CustomMessageBoxButton.YesNo:
                    dlg.ShowSecondary("No", CustomMessageBoxResult.No, isCancel: true);
                    dlg.ShowPrimary("Sí", CustomMessageBoxResult.Yes, isAccept: true);
                    break;
                case CustomMessageBoxButton.YesNoCancel:
                    dlg.ShowExtra("Cancelar", CustomMessageBoxResult.Cancel, isCancel: true);
                    dlg.ShowSecondary("No", CustomMessageBoxResult.No, isCancel: false);
                    dlg.ShowPrimary("Sí", CustomMessageBoxResult.Yes, isAccept: true);
                    break;
            }

            // Mostrar modal
            dlg.ShowDialog();
            return dlg._result;
        }

        // Helpers para configurar botones
        private void ShowPrimary(string text, CustomMessageBoxResult result, bool isAccept = false)
        {
            BtnPrimary.Content = text;
            BtnPrimary.Visibility = Visibility.Visible;
            BtnPrimary.Tag = result;
            if (isAccept) this.AcceptButton(BtnPrimary);
        }

        private void ShowSecondary(string text, CustomMessageBoxResult result, bool isCancel = false)
        {
            BtnSecondary.Content = text;
            BtnSecondary.Visibility = Visibility.Visible;
            BtnSecondary.Tag = result;
            if (isCancel) this.CancelButton(BtnSecondary);
        }

        private void ShowExtra(string text, CustomMessageBoxResult result, bool isCancel = false)
        {
            BtnExtra.Content = text;
            BtnExtra.Visibility = Visibility.Visible;
            BtnExtra.Tag = result;
            if (isCancel) this.CancelButton(BtnExtra);
        }

        // Establecer tecla Enter y Escape
        private void AcceptButton(UIElement btn)
        {
            this.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    (btn as System.Windows.Controls.Button)?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                }
            };
            (btn as System.Windows.Controls.Button).IsDefault = true;
        }

        private void CancelButton(UIElement btn)
        {
            this.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    (btn as System.Windows.Controls.Button)?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                }
            };
            (btn as System.Windows.Controls.Button).IsCancel = true;
        }

        // Click handlers
        private void BtnPrimary_Click(object sender, RoutedEventArgs e)
        {
            _result = (CustomMessageBoxResult)((FrameworkElement)sender).Tag;
            this.Close();
        }

        private void BtnSecondary_Click(object sender, RoutedEventArgs e)
        {
            _result = (CustomMessageBoxResult)((FrameworkElement)sender).Tag;
            this.Close();
        }

        private void BtnExtra_Click(object sender, RoutedEventArgs e)
        {
            _result = (CustomMessageBoxResult)((FrameworkElement)sender).Tag;
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomMessageBoxResult.None;
            this.Close();
        }

        // Funcionalidad para arrastrar desde el header
        private void HeaderBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
