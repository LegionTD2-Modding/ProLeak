using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ProLeakCore
{
    using P = Plugin;
    using C = Constants;
    
    internal static class Constants
    {
        internal static readonly string ClientVersionSupported;
        
        internal static readonly string GatewayFolderPath;
        internal static readonly string GatewayFilename;
        internal static readonly string GatewayModdedFilename;
        internal static readonly List<string> ScriptsFilenames;
        internal static readonly int ScriptsInsertLine;
        
        internal static string GatewayFilePathUI;
        internal static string GatewayModdedFilePathUI;
        internal static string GatewayFilePath;
        internal static string GatewayModdedFilePath;

        internal static List<string> ScriptsFilePaths;
        internal static List<string> ScriptsEmbeddedFilePaths;

        static Constants()
        {
            ClientVersionSupported = "10.03.3";
            
            GatewayFolderPath = Path.Combine(Paths.GameRootPath, "Legion TD 2_Data", "uiresources", "AeonGT");
        
            GatewayFilename = "gateway.html";
            GatewayModdedFilename = $"ProLeak_{GatewayFilename}";
            
            GatewayFilePathUI = $"coui://uiresources/AeonGT/{GatewayFilename}";
            GatewayModdedFilePathUI = $"coui://uiresources/AeonGT/{GatewayModdedFilename}";
            
            GatewayFilePath = Path.Combine(GatewayFolderPath, GatewayFilename);
            GatewayModdedFilePath = Path.Combine(GatewayFolderPath, GatewayModdedFilename);
            
            ScriptsInsertLine = 100;
            ScriptsFilenames = new List<string>{
                "js/api.js",
                "js/settings.js"
            };

            ScriptsFilePaths = new List<string>();
            ScriptsEmbeddedFilePaths = new List<string>();
            
            foreach (var filename in ScriptsFilenames)
            {
                ScriptsFilePaths.Add(Path.Combine(GatewayFolderPath, filename));
                ScriptsEmbeddedFilePaths.Add($"ProLeak.Core.Scripts.{filename}");
            }

        }
    }

    [BepInProcess("Legion TD 2.exe")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

        internal new static ManualLogSource Logger;

        private Traverse _trPresetsOptionsSections;
        private string _configApiClientVersion;

        public void Awake() {
            Logger = base.Logger;
            
            var typeConfigApi = AccessTools.TypeByName("Assets.Api.ConfigApi");
            var trConfigApi = Traverse.Create(typeConfigApi);
            _configApiClientVersion = trConfigApi.Field("ClientVersion").GetValue<string>();
            
            var typePresets = AccessTools.TypeByName("Assets.Presets");
            var trPresets = Traverse.Create(typePresets);
            _trPresetsOptionsSections = trPresets.Field("OptionsSections");

            if (!_configApiClientVersion.Equals(C.ClientVersionSupported)) {
                Logger.LogWarning($"Game version: v{_configApiClientVersion}");
                Logger.LogWarning($"ProLeak built for older game version: v{C.ClientVersionSupported}");
                Logger.LogWarning("The behavior of ProLeak is undefined, but should be fine");
            }

            try {
                DeleteModdedGateway();
                CreateModdedGateway();
                Patch();
            }
            catch (Exception e) {
                Logger.LogError($"Error while create modded gateway or while patching: {e}");
                throw;
            }
            
            Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        public void OnDestroy() {
            UnPatch();
            DeleteModdedGateway();
        }

        private void Patch() {
            /*
            _trPresetsOptionsSections
                .Method("Add", new object[]{C.CfgLegionField, C.CfgLegionSection})
                .GetValue();*/
            _harmony.PatchAll(_assembly);
        }

        private void UnPatch() {
            /*
            if (_trPresetsOptionsSections
                .Method("ContainsKey", new object[]{C.CfgLegionField})
                .GetValue<bool>()) 
            {
                _trPresetsOptionsSections.Method("Remove", new object[]{C.CfgLegionField}).GetValue();
            }*/
            _harmony.UnpatchSelf();
        }
        
        private void CreateModdedGateway() {
            var lines = File.ReadAllLines(C.GatewayFilePath);
            var resStream = _assembly.GetManifestResourceStream(C.GatewayEmbedded);
            using (var r = new StreamReader(resStream ?? throw new FileNotFoundException(C.GatewayEmbedded))) {
                lines[C.ScriptsInsertLine] = r.ReadToEnd() + Environment.NewLine + lines[C.ScriptsInsertLine];
            }
            File.WriteAllLines(C.GatewayModdedFilePath, lines);
        }

        private static void DeleteModdedGateway() {
            if (File.Exists(C.GatewayModdedFilePathUI)) {
                File.Delete(C.GatewayModdedFilePathUI);
            }
        }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [HarmonyPatch]
    internal static class PatchSendCreateView
    {
        private static Type _typeCoherentUIGTView;

        [HarmonyPrepare]
        private static void Prepare() {
            _typeCoherentUIGTView = AccessTools.TypeByName("CoherentUIGTView");
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() {
            return AccessTools.Method(_typeCoherentUIGTView, "SendCreateView");
        }

        [HarmonyPrefix]
        private static bool SendCreateViewPre(ref string ___m_Page) {
            if (___m_Page.Equals(C.GatewayFile)) {
                ___m_Page = C.GatewayFileModded;
            }
            return true;
        }
    }
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [HarmonyPatch]
    internal static class PatchLoadOptions
    {
        private static Type _typeHudOptions;
        private static Type _typeOptionValue;
        private static Traverse _trHudApi;

        [HarmonyPrepare]
        private static void Prepare() {
            _typeHudOptions = AccessTools.TypeByName("Assets.Features.Hud.HudOptions");
            _typeOptionValue = AccessTools.Inner(_typeHudOptions, "OptionValue");

            _trHudApi = Traverse.Create(AccessTools.TypeByName("Assets.Api.HudApi"));
        }
        
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() {
            return AccessTools.Method(_typeHudOptions, "LoadOptions");
        }
        
        [HarmonyPostfix]
        private static void LoadOptionsPost(
            ref object ___config,
            ref Dictionary<string, object> ___options,
            ref Dictionary<string, Action<string>> ___OptionsHandlers) {
            
            if (!___OptionsHandlers.ContainsKey(C.CfgLegionField)) {
                ___OptionsHandlers.Add(C.CfgLegionField, delegate(string value) {

                    var argsTriggerHudEvent = new object[] {
                        C.CfgLegionFieldEvent,
                        Math.Max(0, Array.IndexOf(C.CfgLegionPossibleValues, value))
                    };

                    _trHudApi.Method("TriggerHudEvent", argsTriggerHudEvent).GetValue();
                });
                P.Logger.LogInfo($"Custom mod option {C.CfgLegionField} handler assigned");
            }
            
            var optionValue = Activator.CreateInstance(_typeOptionValue);

            var argsLoadString = new object[] {
                C.CfgLegionField,
                C.CfgLegionDefaultValue,
                C.CfgLegionPossibleValues
            };

            var strFromCfg = Traverse.Create(___config)
                .Method("LoadString", argsLoadString)
                .GetValue<string>();

            Traverse.Create(optionValue).Field("value").SetValue(strFromCfg);
            Traverse.Create(optionValue).Field("defaultValue").SetValue(C.CfgLegionDefaultValue);
            Traverse.Create(optionValue).Field("optionType").SetValue("choice");
            Traverse.Create(optionValue).Field("possibleValues").SetValue(C.CfgLegionPossibleValues);
            
            if (___options.ContainsKey(C.CfgLegionField)) {
                ___options[C.CfgLegionField] = optionValue;
                
                P.Logger.LogInfo($"Custom mod option {C.CfgLegionField} loaded");
                return;
            }
            
            ___options.Add(C.CfgLegionField, optionValue);
            P.Logger.LogInfo($"Custom mod option {C.CfgLegionField} added");
        }
        
    }
    

}
