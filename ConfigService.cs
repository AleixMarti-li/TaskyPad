using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskyPad
{
    public class ConfigService
    {
        public string _dataPath;
        public Config _configuracion = new Config();

        public ConfigService() 
        {
            _dataPath = LoadConfigPath();
            LoadConfigJSON();
        }

        public void LoadConfigJSON()
        {
            if (!File.Exists($"{_dataPath}\\configuration.json"))
            {
                _configuracion = new Config();
                return;
            }

            string conteindoJSON = File.ReadAllText($"{_dataPath}\\configuration.json");

            if (string.IsNullOrEmpty(conteindoJSON)) return;

            Config? ConfigRecuperada = JsonSerializer.Deserialize<Config>(conteindoJSON);

            _configuracion = ConfigRecuperada ?? new Config();
        }

        public string LoadConfigPath()
        {
            return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TaskyPad",
                "Configuration"
            );
        }

        public void SaveConfigJSON()
        {
            string PathFile = System.IO.Path.Combine(_dataPath, "configuration.json");

            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            if (!File.Exists(PathFile))
            {
                File.Create(PathFile).Dispose();
            }

            string json = JsonSerializer.Serialize(_configuracion);
            File.WriteAllText(PathFile, json);
        }
    }
}
