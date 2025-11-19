using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Velopack;
using Velopack.Sources;
using Serilog;

namespace TaskyPad
{
    public class UpdateManager
    {
        private readonly Velopack.UpdateManager updateManager;
        private UpdateInfo? updateInfo;

        public UpdateManager() 
        {
            Log.Information("UpdateManager constructor called");
            try
            {
                updateManager = new Velopack.UpdateManager(new GithubSource("https://github.com/AleixMarti-li/TaskyPad",null,false));
                Log.Information("UpdateManager initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing UpdateManager");
                throw;
            }
        }

        public async Task<returnMessageUpdateInfo> CheckActualizacionDisponible() 
        {
            Log.Information("CheckActualizacionDisponible called");
            try
            {
#if DEBUG
                Log.Information("Running in DEBUG mode - returning test version");
                return new returnMessageUpdateInfo(true, "DEV");
#endif

                Log.Information("Checking if updateManager is initialized");
                if (updateManager is null)
                {
                    Log.Error("UpdateManager is null");
                    throw new Exception("update manager no instanciado");
                }
                
                Log.Information("Calling CheckForUpdatesAsync");
                updateInfo = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
                Log.Information("CheckForUpdatesAsync completed");
                
                if (updateInfo is null)
                {
                    Log.Information("No updates available");
                    return new returnMessageUpdateInfo(false);
                }
                
                Log.Information("Update available - Version: {Version}", updateInfo.TargetFullRelease.Version.ToString());
                return new returnMessageUpdateInfo(true, updateInfo.TargetFullRelease.Version.ToString());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for updates");
                return new returnMessageUpdateInfo($"Error: {ex.Message}");
            }
        }

        public async Task ForceUpdate()
        {
            Log.Information("ForceUpdate called");
            try
            {
                if (updateManager is null)
                {
                    Log.Error("Cannot apply updates: UpdateManager is null");
                    Debug.WriteLine("Cannot apply updates: UpdateManager is null");
                    return;
                }

#if !DEBUG
                if (updateInfo is null)
                {
                    Log.Error("Cannot apply updates: update info is null");
                    Debug.WriteLine("Cannot apply updates: update is null");
                    return;
                }
#endif

                Log.Information("Downloading updates");
                await updateManager.DownloadUpdatesAsync(updateInfo);
                Log.Information("Updates downloaded, applying and restarting");
                updateManager.ApplyUpdatesAndRestart(updateInfo);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during update process");
                Debug.WriteLine($"Error during update: {ex.Message}");
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
