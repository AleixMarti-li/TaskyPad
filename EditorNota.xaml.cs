using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    /// Lógica de interacción para EditorNota.xaml
    /// </summary>
    public partial class EditorNota : Window
    {
        private string _nombreNota;
        private MainWindow _ventanaPrincipal;
        public EditorNota(string nombre, MainWindow ventanaPrincipal)
        {
            InitializeComponent();
            _nombreNota = nombre;
            _ventanaPrincipal = ventanaPrincipal;
            CargarContenidoNota();
        }

        private void BtnCancelar(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnEliminar(object sender, RoutedEventArgs e)
        {
           MessageBoxResult eliminarNota = MessageBox.Show($"Estas seguro de eliminar la nota de {_nombreNota}? esta accion no se puede deshacer", "Advertencia", MessageBoxButton.YesNo,MessageBoxImage.Warning);
            if (eliminarNota == MessageBoxResult.No) return;
            string path = $"notas/{_nombreNota}.txt";
            File.Delete(path);
            _ventanaPrincipal.RecuperarNotas();
            this.Close();
        }

        private void BtnGuardar(object sender, RoutedEventArgs e)
        {
            TextRange texto = new TextRange(NotaTextBox.Document.ContentStart, NotaTextBox.Document.ContentEnd);
            using (FileStream fs = new FileStream($"notas/{_nombreNota}.txt", FileMode.Create))
            {
                texto.Save(fs, DataFormats.Rtf);
            }
            this.Close();
        }

        private void CargarContenidoNota()
        {

            TextRange texto = new TextRange(NotaTextBox.Document.ContentStart, NotaTextBox.Document.ContentEnd);
            using (FileStream fs = new FileStream($"notas/{_nombreNota}.txt", FileMode.Open))
            {
                texto.Load(fs, DataFormats.Rtf);
            }
        }

        private void BtnBold_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleBold.Execute(null, NotaTextBox);
        }

        private void BtnItalic_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleItalic.Execute(null, NotaTextBox);
        }

        private void BtnUnderline_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleUnderline.Execute(null, NotaTextBox);
        }

        private void FontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeBox.SelectedItem is ComboBoxItem item)
                NotaTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, Convert.ToDouble(item.Content));
        }

        private void FontFamilyBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontFamilyBox.SelectedItem is ComboBoxItem item)
                NotaTextBox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(item.Content.ToString()));
        }

        private void NotaTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // ---- Negrita ----
            var bold = NotaTextBox.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            BtnBold.IsChecked = (bold != DependencyProperty.UnsetValue) && bold.Equals(FontWeights.Bold);

            // ---- Cursiva ----
            var italic = NotaTextBox.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            BtnItalic.IsChecked = (italic != DependencyProperty.UnsetValue) && italic.Equals(FontStyles.Italic);

            // ---- Subrayado ----
            var underline = NotaTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            BtnUnderline.IsChecked = (underline != DependencyProperty.UnsetValue) && underline.Equals(TextDecorations.Underline);

            // ---- Tamaño de fuente ----
            var size = NotaTextBox.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            if (size != DependencyProperty.UnsetValue)
            {
                foreach (ComboBoxItem item in FontSizeBox.Items)
                {
                    if (item.Content.ToString() == ((double)size).ToString())
                    {
                        FontSizeBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // ---- Familia de fuente ----
            var family = NotaTextBox.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
            if (family != DependencyProperty.UnsetValue)
            {
                foreach (ComboBoxItem item in FontFamilyBox.Items)
                {
                    if (item.Content.ToString() == family.ToString())
                    {
                        FontFamilyBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }
    }
}
