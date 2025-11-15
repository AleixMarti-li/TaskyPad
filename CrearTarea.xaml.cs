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
            NotificarTarea.IsChecked = _existingTarea.notificar;
            TimePickerTarea.Value = new DateTime(
                _existingTarea.fecha.Year, _existingTarea.fecha.Month, _existingTarea.fecha.Day,
                _existingTarea.fecha.Hour,
                _existingTarea.fecha.Minute,
                _existingTarea.fecha.Second
            );
        }

        private void BtnCrearTarea_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            if (_existingTarea is null)
            {
                CrearTareaLogic();
            }
            else
            {
                UpdateTareaLogic();
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool ValidateForm()
        {
            List<string> errores = new List<string>();

            // Validar título
            if (string.IsNullOrWhiteSpace(TituloTarea.Text))
                errores.Add("• El título de la tarea es obligatorio");

            // Validar descripción
            if (string.IsNullOrWhiteSpace(DescripcionTarea.Text))
                errores.Add("• La descripción es obligatoria");

            // Validar fecha
            if (FechaTarea.SelectedDate == null)
                errores.Add("• Debes seleccionar una fecha");
            else if (FechaTarea.SelectedDate.Value.Date < DateTime.Now.Date)
                errores.Add("• La fecha no puede ser anterior a hoy");

            // Validar hora
            if (TimePickerTarea.Value == null)
                errores.Add("• Debes seleccionar una hora");

            // Mostrar alerta si hay errores
            if (errores.Count > 0)
            {
                MostrarAlerta(errores);
                return false;
            }

            // Ocultar alerta si la validación es correcta
            OcultarAlerta();
            return true;
        }

        private void MostrarAlerta(List<string> errores)
        {
            AlertMessage.Text = string.Join("\n", errores);
            AlertBorder.Visibility = Visibility.Visible;
        }

        private void OcultarAlerta()
        {
            AlertBorder.Visibility = Visibility.Collapsed;
            AlertMessage.Text = "";
        }

        private void UpdateTareaLogic()
        {
            if (_existingTarea is null) return;
            _existingTarea.titulo = TituloTarea.Text;
            _existingTarea.descripcion = DescripcionTarea.Text;
            _existingTarea.notificar = NotificarTarea.IsChecked ?? false;
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
            bool notificarTarea = NotificarTarea.IsChecked ?? false;
            DateTime fechaTarea = FechaTarea.SelectedDate.Value;
            fechaTarea = new DateTime(
                fechaTarea.Year,
                fechaTarea.Month,
                fechaTarea.Day,
                TimePickerTarea.Value.Value.Hour,
                TimePickerTarea.Value.Value.Minute,
                TimePickerTarea.Value.Value.Second
            );
            Tarea NuevaTarea = new Tarea(tituloTarea, descripcionTarea, fechaTarea, notificarTarea);
            _ventanaPrincipal.AddTareaToList(NuevaTarea);
            this.Close();
        }
    }
}
