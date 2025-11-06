using System;
using System.Collections.Generic;
using System.Text;

namespace TaskyPad
{
    public class Tarea
    {
        public EstadoTarea estado { get; set; } = EstadoTarea.None;
        public string titulo { get; set; }
        public string descripcion { get; set; }
        public DateTime fecha { get; set; }
        public string idTarea { get; set; }
        public DateTime ultimaModificacion { get; set; }

        public Tarea(string titulo, string descripcion, DateTime fecha)
        {
            this.titulo = titulo;
            this.descripcion = descripcion;
            this.fecha = fecha;
            idTarea = Guid.NewGuid().ToString();
            ultimaModificacion = DateTime.Now;
        }
    }

    public enum EstadoTarea 
    { 
        None,
        InProgress,
        Done
    }
}
