using System;
using System.Collections.Generic;
using System.Security.AccessControl;
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
        private Tarea? _existingTarea;
        public CrearTarea(MainWindow ventanaPrincipal)
        {
            InitializeComponent();
            _ventanaPrincipal = ventanaPrincipal;
        }

        public CrearTarea(MainWindow ventanaPrincipal, Tarea existingTarea)
        {
            _ventanaPrincipal = ventanaPrincipal;
            _existingTarea = existingTarea;
            InitializeComponent();
            LoadExistingTaskToUI();
        }

        private void LoadExistingTaskToUI()
        {
            if (_existingTarea == null)
                return;

            TituloTarea.Text = _existingTarea.titulo;
            DescripcionTarea.Text = _existingTarea.descripcion;
            FechaTarea.SelectedDate = _existingTarea.fecha.Date;
            TimePickerTarea.Value = new DateTime(
                _existingTarea.fecha.Year, _existingTarea.fecha.Month, _existingTarea.fecha.Day,
                _existingTarea.fecha.Hour,
                _existingTarea.fecha.Minute,
                _existingTarea.fecha.Second
            );
        }

        private void BtnCrearTarea_Click(object sender, RoutedEventArgs e)
        {
            if (_existingTarea is null)
            {
                CrearTareaLogic();
            } else
            {
                UpdateTareaLogic();
            }
            
        }

        private void UpdateTareaLogic()
        {
            if (_existingTarea is null) return;
            _existingTarea.titulo = TituloTarea.Text;
            _existingTarea.descripcion = DescripcionTarea.Text;
            DateTime fechaTarea = FechaTarea.SelectedDate.Value;
            _existingTarea.fecha = new DateTime(
                fechaTarea.Year,
                fechaTarea.Month,
                fechaTarea.Day,
                TimePickerTarea.Value.Value.Hour,
                TimePickerTarea.Value.Value.Minute,
                TimePickerTarea.Value.Value.Second
            );
            _ventanaPrincipal.UpdateTareaToList(_existingTarea);
            this.Close();
        }

        private void CrearTareaLogic()
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
            Tarea NuevaTarea = new Tarea(tituloTarea, descripcionTarea, fechaTarea);
            _ventanaPrincipal.AddTareaToList(NuevaTarea);
            this.Close();
        }
    }
}
