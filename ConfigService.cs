using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace TaskyPad
{
    public class ConfigService
    {
        public string _dataPath;
        public Config _configuracion = new Config();

        public ConfigService() 
        {
            Log.Information("ConfigService initialized");
            _dataPath = LoadConfigPath();
            Log.Information("Config path: {ConfigPath}", _dataPath);
            LoadConfigJSON();
            _configuracion.ExecuteWinStart();
        }

        public void LoadConfigJSON()
        {
            Log.Information("Loading configuration from JSON");
            
            if (!File.Exists($"{_dataPath}\\configuration.json"))
            {
                Log.Information("Configuration file does not exist, creating default configuration");
                _configuracion = new Config();
                return;
            }

            string conteindoJSON = File.ReadAllText($"{_dataPath}\\configuration.json");

            if (string.IsNullOrEmpty(conteindoJSON))
            {
                Log.Warning("Configuration file is empty");
                return;
            }

            try
            {
                Config? ConfigRecuperada = JsonSerializer.Deserialize<Config>(conteindoJSON);
                _configuracion = ConfigRecuperada ?? new Config();
                
                Log.Information("Configuration loaded successfully");
                Log.Debug("Configuration - Encrypt enabled: {EncryptEnabled}, Auto start: {AutoStart}", 
                    _configuracion.enableEncrypt, _configuracion.iniciarAuto);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deserializing configuration from JSON");
                _configuracion = new Config();
            }
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
            Log.Information("Saving configuration to JSON");
            
            string PathFile = System.IO.Path.Combine(_dataPath, "configuration.json");

            if (!Directory.Exists(_dataPath))
            {
                Log.Information("Configuration directory does not exist, creating: {Path}", _dataPath);
                Directory.CreateDirectory(_dataPath);
            }

            if (!File.Exists(PathFile))
            {
                Log.Information("Configuration file does not exist, creating: {Path}", PathFile);
                File.Create(PathFile).Dispose();
            }

            try
            {
                string json = JsonSerializer.Serialize(_configuracion);
                File.WriteAllText(PathFile, json);
                
                Log.Information("Configuration saved successfully to {Path}", PathFile);
                Log.Debug("Saved configuration - Encrypt enabled: {EncryptEnabled}, Auto start: {AutoStart}", 
                    _configuracion.enableEncrypt, _configuracion.iniciarAuto);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving configuration to JSON");
            }
        }
    }
}
