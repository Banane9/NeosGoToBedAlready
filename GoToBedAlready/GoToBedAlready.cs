using BaseX;
using CloudX.Shared;
using FrooxEngine;
using FrooxEngine.LogiX;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GoToBedAlready
{
    public class GoToBedAlready : NeosMod
    {
        private static bool? cloudStartupValue = null;

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> CloudVariablePath = new ModConfigurationKey<string>("CloudVariablePath", "Cloud Variable Path to check for quitting the game. Default value is just a broadcast variable that won't change. Must be a bool.", () => "U-Banane9.GoToBedAlready");

        private static ModConfiguration Config;

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> Enable = new ModConfigurationKey<bool>("Enable", "Enable the game quitting when the cloud variable changes value.", () => true);

        public override string Author => "Banane9";
        public override string Link => "https://github.com/Banane9/NeosGoToBedAlready";
        public override string Name => "GoToBedAlready";
        public override string Version => "1.0.0";

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            Config.OnThisConfigurationChanged += OnConfigurationChanged;
            Config.Save(true);

            Engine.Current.OnReady += () => Userspace.Current.RunInSeconds(120, CloudVariablePolling);
        }

        private static void CloudVariablePolling()
        {
            var worker = Userspace.Current;

            if (Config.GetValue(Enable))
            {
                worker.StartTask(async delegate ()
                {
                    var proxy = worker.Cloud.Variables.RequestProxy(worker.LocalUser.UserID, Config.GetValue(CloudVariablePath));

                    await proxy.Refresh();

                    if (proxy.State == CloudVariableState.Invalid || proxy.State == CloudVariableState.Unregistered
                     || proxy.Identity.path != Config.GetValue(CloudVariablePath))
                    {
                        Error("Failed to poll cloud variable or variable path changed while polling.");
                    }
                    else
                    {
                        var value = proxy.ReadValue<bool>();

                        if (!cloudStartupValue.HasValue)
                            cloudStartupValue = value;

                        if (value ^ cloudStartupValue.Value)
                            Engine.Current.Shutdown();
                    }
                });
            }

            if (!Engine.Current.ShutdownRequested)
                worker.RunInSeconds(5, CloudVariablePolling);
        }

        private void OnConfigurationChanged(ConfigurationChangedEvent configurationChangedEvent)
        {
            if (!configurationChangedEvent.Key.Equals(CloudVariablePath))
                return;

            updateCloudProxy();
        }

        private void updateCloudProxy()
        {
            if (!CloudVariableHelper.IsValidPath(Config.GetValue(CloudVariablePath)))
            {
                Error("Invalid Cloud Variable Path");
                Config.Set(Enable, false);
            }
            else
            {
                Msg("Valid Cloud Variable Path");
                cloudStartupValue = null;
            }
        }
    }
}