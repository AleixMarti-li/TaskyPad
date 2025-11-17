using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskyPad
{
    public class Config
    {
        public bool iniciarAuto { get; set; } = false;

        public bool enableEncrypt { get; set; } = true;
        public string? passwordEncrypt { get; set; } = "123";
    }
}
