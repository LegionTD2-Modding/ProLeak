using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using ProLeakCore;

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
    
    public abstract class ProLeakMod
    {
        public abstract void ModInit();

        // Set a restrictive enough type T
        public T RegisterSetting<T>(string name, T value, string nameAlias) {
            this.Settings.Add(new ModSetting<T>(name, value, nameAlias));
            return value;
        }
        
        // RegisterEvent()

        public List<string> GetModScriptsFiles() {
            return this.Scripts.Select(s => s.ScriptFile).ToList();
        }

        public ModSetting<T> GetSetting<T>(string name) {
            return this.Settings.Find<T>(s => s.Name.Equals(name));
        }
        
        protected ProLeakMod() {
            // this.Core = null;
            // Find a good design for the exchange between the API and the Core
            // protected ProLeakCore Core { get; private set; }
            
            var derivedType = this.GetType();
            this.Infos = derivedType.GetCustomAttribute<ModInfos>();
            this.Scripts = derivedType.GetCustomAttributes<ModScript>().ToList();
            this.Settings = new List<ModSetting<Type>>();
        }

        // Design the contact to the Core
        // protected ProLeakCore Core { get; private set; }
        
        protected ModInfos Infos { get; private set; }
        public List<ModScript> Scripts { get; private set; }
        public List<ModSetting<Type>> Settings { get; private set; }
    }


}
