using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace ProLeak
{
    using Core = ProLeakCore.ProLeakCore;
    
    [AttributeUsage(AttributeTargets.Class)]
    public class ModInfos : Attribute
    {
        public ModInfos(string name, string author, string version, string guid, string description) {
            this.Name = name;
            this.Author = author;
            this.Guid = guid;
            this.Description = description;
            try {
                this.Version = new Version(version);
            } catch {
                this.Version = new Version(0, 0, 0, 0);
            }
        }

        public string Name { get; private set; }
        public string Author { get; private set; }
        public Version Version { get; private set; }
        public string Guid { get; private set; }
        public string Description { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModScript : Attribute
    {
        public ModScript(string filepath) {
            ScriptFile = filepath;
        }
        
        public string ScriptFile { get; private set; }
    }
    
    public interface IModSetting
    {
        string Name { get; }
    }

    public class ModSetting<T> : IModSetting
    {
        public string Name { get; set; }
        public T Value { get; set; }
        public T DefaultValue { get; private set; }
        public List<string> Aliases { get; set; }

        public ModSetting(string settingName, T value, List<string> aliases)
        {
            Name = settingName;
            Value = value;
            DefaultValue = value;
            Aliases = aliases;
        }
    }

    public abstract class ProLeakMod : MonoBehaviour
    {
        public abstract void ModInit();
        public abstract void ModCleanup();

        public bool RegisterSetting<T>(string settingName, T value, List<string> aliases)
        {
            if (Settings.ContainsKey(settingName)) {
                return false;
            }
            var setting = new ModSetting<T>(settingName, value, aliases);
            Settings.Add(settingName, setting);
            return true;
        }
        
        public bool UnregisterSetting<T>(string settingName)
        {
            if (!Settings.ContainsKey(settingName)) {
                return false;
            }
            Settings.Remove(settingName);
            return true;
        }
        
        public bool SetSettingValue<T>(string settingName, T value)
        {
            if (!Settings.TryGetValue(settingName, out var setting) || setting is not ModSetting<T> typedSetting) {
                return false;
            }
            typedSetting.Value = value;
            Settings[settingName] = typedSetting;
            return true;
        }

        public bool GetSettingValue<T>(string settingName, out T value)
        {
            if (Settings.TryGetValue(settingName, out var setting) && setting is ModSetting<T> typedSetting)
            {
                value = typedSetting.Value;
                return true;
            }
            value = default(T);
            return false;
        }

        protected ProLeakMod() {
            var derivedType = this.GetType();
            this.Infos = derivedType.GetCustomAttribute<ModInfos>();
            this.Scripts = derivedType.GetCustomAttributes<ModScript>().ToList();
            this.Settings = new Dictionary<string, IModSetting>();
        }

        private void Awake() {
            var registered = _core.SendRegisterModRequest(this);
            if (!registered) {
                Log("Unable to register mod with Core", LogLevel.Fatal);
                this.enabled = false;
                return;
            }
            this.ModInit();
        }

        private void OnDestroy() {
            _core.SendUnregisterModRequest(this);
            this.ModCleanup();
        }

        private void OnDisable() {
            _core.SendUnregisterModRequest(this);
            this.ModCleanup();
        }

        public void Log(string data, LogLevel level = LogLevel.Info) {
            _logger.Log(level, $"({Infos.Name}) {data}");
        }

        public ModInfos Infos { get; private set; }
        public List<ModScript> Scripts { get; private set; }
        public Dictionary<string, IModSetting> Settings { get; private set; }
        
        private readonly Core _core = Core.Instance;
        private readonly ManualLogSource _logger = Core.Logger;
    }
}
