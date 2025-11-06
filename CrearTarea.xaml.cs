using System;
using System.Collections.Generic;
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
    /// Lógica de interacción para CrearTarea.xaml
    /// </summary>
    public partial class CrearTarea : Window
    {
        private MainWindow _ventanaPrincipal;
        public CrearTarea(MainWindow ventanaPrincipal)
        {
            InitializeComponent();
            _ventanaPrincipal = ventanaPrincipal;
        }

        private void BtnCrearTarea_Click(object sender, RoutedEventArgs e)
        {
            string tituloTarea = TituloTarea.Text;
            string descripcionTarea = DescripcionTarea.Text;
            DateTime fechaTarea = FechaTarea.SelectedDate.Value;
            fechaTarea = new DateTime(
                fechaTarea.Year,
                fechaTarea.Month,
                fechaTarea.Day,
                TimePickerTarea.Value.Value.Hour,
                TimePickerTarea.Value.Value.Minute,
                TimePickerTarea.Value.Value.Second
            );
            Tarea NuevaTarea = new Tarea(tituloTarea,descripcionTarea,fechaTarea);
            _ventanaPrincipal.AddTareaToList(NuevaTarea);
            this.Close();
        }
    }
}
