using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        internal static readonly int ScriptsInsertLine;
        internal static readonly string GatewayFilePathUI;
        internal static readonly string GatewayModdedFilePathUI;
        internal static readonly string GatewayFilePath;
        internal static readonly string GatewayModdedFilePath;
        internal static readonly List<string> ScriptsEmbeddedFilePaths;

        static Constants()
        {
            ClientVersionSupported = "10.03.3";
            
            var gatewayFolderPath = Path.Combine(Paths.GameRootPath, "Legion TD 2_Data", "uiresources", "AeonGT");
        
            const string gatewayFilename = "gateway.html";
            const string gatewayModdedFilename = $"ProLeak_{gatewayFilename}";
            
            GatewayFilePathUI = $"coui://uiresources/AeonGT/{gatewayFilename}";
            GatewayModdedFilePathUI = $"coui://uiresources/AeonGT/{gatewayModdedFilename}";
            
            GatewayFilePath = Path.Combine(gatewayFolderPath, gatewayFilename);
            GatewayModdedFilePath = Path.Combine(gatewayFolderPath, gatewayModdedFilename);
            
            ScriptsInsertLine = 100;
            var scriptsFilenames = new List<string>{
                "api.js",
                "settings.js"
            };

            ScriptsEmbeddedFilePaths = scriptsFilenames.Select(f => $"ProLeakCore.scripts.{f}").ToList();
            ScriptsEmbeddedFilePaths.Reverse();

        }
    }

    [BepInProcess("Legion TD 2.exe")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);
        
        //internal;

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
            // TODO
            /*
            _trPresetsOptionsSections
                .Method("Add", new object[]{C.CfgLegionField, C.CfgLegionSection})
                .GetValue();*/
            _harmony.PatchAll(_assembly);
        }

        private void UnPatch() {
            // TODO
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
            
            foreach (var embeddedFilePath in C.ScriptsEmbeddedFilePaths)
            {
                using TextReader r = new StreamReader(_assembly.GetManifestResourceStream(embeddedFilePath)
                                                      ?? throw new FileNotFoundException(embeddedFilePath));
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
            if (___m_Page.Equals(C.GatewayFilePathUI)) {
                ___m_Page = C.GatewayModdedFilePathUI;
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
            // TODO
            /*
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
            P.Logger.LogInfo($"Custom mod option {C.CfgLegionField} added");*/
        }
        
    }
}
