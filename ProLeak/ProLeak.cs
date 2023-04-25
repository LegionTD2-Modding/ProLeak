using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ProLeakCore;
using Unity.Baselib.LowLevel;
using UnityEngine;
using Logger = UnityEngine.Logger;

namespace ProLeak
{
   
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
    
    public class ModSetting<T>
    {
        public ModSetting(string name, T value, List<string> aliases) {
            this.Name = name;
            this.ValueType = typeof (T);
            this.DefaultValue = value;
            this.Value = value;
            this.Aliases = aliases;
        }

        public string Name { get; private set; }
        public Type ValueType { get; private set; }
        public T Value { get; private set; }
        public T DefaultValue { get; private set; }
        public List<string> Aliases { get; private set; }

        public void ResetToDefaultValue() {
            this.Value = this.DefaultValue;
        }

        public void SetValue(T value) {
            this.Value = value;
        }

        public T GetValue() {
            return this.Value;
        }

        public string GetAliasedName() {
            return this.Aliases.Count <= 0 ? "" : this.Aliases[0];
        }
        
        public string GetAliasedValue(int value) {
            return this.Aliases.Count <= value ? "" : this.Aliases[value];
        }
    }
    
    public abstract class ProLeakMod : MonoBehaviour
    {
        public abstract void ModInit();
        
        /*
         // Set a restrictive enough type T
        public T RegisterSetting<T>(string name, T value, string nameAlias) {
            //this.Settings.Add(new ModSetting<T>(name, value, nameAlias));
            return value;
        }*/
        
        // RegisterEvent()

        /*
        public ModSetting<T> GetSetting<T>(string name) {
            return this.Settings.Find<T>(s => s.Name.Equals(name));
        }*/
        
        protected ProLeakMod() {
            var derivedType = this.GetType();
            this.Infos = derivedType.GetCustomAttribute<ModInfos>();
            this.Scripts = derivedType.GetCustomAttributes<ModScript>().ToList();
            this.Settings = new List<ModSetting<Type>>();
        }

        private void Awake() {
            var registered = _core.SendRegisterModRequest(this);
            if (!registered) {
                Log("Unable to register mod with Core", LogLevel.Error);
                return;
            }
            this.ModInit();
        }

        public void Log(string data, LogLevel level = LogLevel.Info) {
            _logger.Log(level, $"({Infos.Name}) {data}");
        }
        
        // Since parameter Scripts is 'private set', is this GetScripts useful in any way ?
        public IReadOnlyList<ModScript> GetScripts() {
            return this.Scripts.AsReadOnly();
        }
        
        public ModInfos Infos { get; private set; }
        public List<ModScript> Scripts { get; private set; }
        public List<ModSetting<Type>> Settings { get; private set; }
        
        private readonly ProLeakCore.ProLeakCore _core = ProLeakCore.ProLeakCore.Instance;
        private readonly ManualLogSource _logger = ProLeakCore.ProLeakCore.Logger;
    }


}
