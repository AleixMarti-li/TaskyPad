using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TaskyPad
{
    public class NotificationService
    {
        public void SendWindowsTaskNotificacionEndTime(Tarea item)
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var iconPath = Path.Combine(exePath!, "logo.ico");

            new ToastContentBuilder()
                .AddArgument("taskId", item.idTarea)


                .AddAppLogoOverride(new Uri(iconPath))

                .AddText(item.titulo, hintMaxLines: 1)
                .AddText("Tarea pendiente", hintStyle: AdaptiveTextStyle.Subtitle)
                .AddText(item.descripcion)
#if DEBUG
                .AddText($"{item.idTarea}")
#endif

                .AddButton(new ToastButton()
                    .SetContent("+ '1")
                    .AddArgument("action", "posponer=1")
                    .SetBackgroundActivation())

                .AddButton(new ToastButton()
                    .SetContent("+ '5")
                    .AddArgument("action", "posponer=5")
                    .SetBackgroundActivation())

                .AddButton(new ToastButton()
                    .SetContent("+ '10")
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
