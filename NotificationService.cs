using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskyPad
{
    public class NotificationService
    {
        public void SendWindowsTaskNotificacionEndTime(Tarea item)
        {
            new ToastContentBuilder()
                .AddArgument("taskId", item.idTarea)
                .AddText($"Tarea Por Hacer: {item.titulo}")
                .AddText($"{item.descripcion}")
#if DEBUG
                .AddText($"{item.idTarea}")
#endif

                .AddButton(new ToastButton()
                    .SetContent("Posponer '10")
                    .AddArgument("action", "posponer=10")
                    .SetBackgroundActivation())

                .AddButton(new ToastButton()
                    .SetContent("Hecho")
                    .AddArgument("action", "done")
                    .SetBackgroundActivation())

                .Show();
        }
    }
}
