using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Velopack;
using Velopack.Sources;

namespace TaskyPad
{
    public class UpdateManager
    {
        private readonly Velopack.UpdateManager updateManager;
        private UpdateInfo? updateInfo;

        public UpdateManager() 
        {
            updateManager = new Velopack.UpdateManager(new GithubSource("https://github.com/AleixMarti-li/TaskyPad",null,false));
        }

        public async Task<returnMessageUpdateInfo> CheckActualizacionDisponible() 
        {
            try
            {
                if (updateManager is null) throw new Exception("update manager no instanciado");
                updateInfo = await updateManager.CheckForUpdatesAsync().ConfigureAwait(true);
                if (updateInfo is null) return new returnMessageUpdateInfo(false);
                return new returnMessageUpdateInfo(true, updateInfo.TargetFullRelease.Version.ToString());
            }
            catch (Exception ex)
            {
                return new returnMessageUpdateInfo($"Error: {ex.Message}");
            }
        }
    }

    public class returnMessageUpdateInfo 
    {
        public bool updateAvaliable = false;
        public string? version;
        public string? messageError;

        public returnMessageUpdateInfo(bool udpateAvaliable) 
        {
            this.updateAvaliable = udpateAvaliable;
        }

        public returnMessageUpdateInfo(bool udpateAvaliable, string version) 
        {
            this.updateAvaliable = udpateAvaliable;
            this.version = version;
        }

        public returnMessageUpdateInfo(string messageError) 
        {
            this.messageError = messageError;
        }
    }
}
