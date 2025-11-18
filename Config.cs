using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskyPad
{
    public class Config
    {
        public bool iniciarAuto { get; set; } = true;
        public bool enableEncrypt { get; set; }
        public string? passwordEncrypt { get; set; }

        public void ExecuteWinStart()
        {
            if (!iniciarAuto) return;

            AutoStartService autoStartService = new AutoStartService();
            autoStartService.EnableAutoStart();
        }
    }
}
