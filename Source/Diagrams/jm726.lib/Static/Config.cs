#region Namespace imports

using System;
using System.Configuration;
using System.Collections.Generic;

#endregion

namespace common.config {

    /// <summary>
    ///   Static class to give access to all the keys.
    /// 
    ///   Also has a utility method for parsing integer values from the config events.
    /// </summary>
    public static partial class Config {
        public enum Section { Model, View, Algorithm, Common, MRM, Control, ControlToggle, ControlButton, Bootstrap }

        private static readonly Dictionary<Section, Dictionary<string, string>> Variables = new Dictionary<Section, Dictionary<string, string>>();

        /// <summary>
        /// Manually sets values. Values set this way override the values set in the configuration file. Should only be used for testing.
        /// </summary>
        /// <param name="setting">The setting to set the value of.</param>
        /// <param name="section">The section in which the setting appears.</param>
        /// <param name="value">The value to set for the setting.</param>
        public static void AddSetting(string setting, Section section, string value) {
            if (!Variables.ContainsKey(section))
                Variables.Add(section, new Dictionary<string, string>());
            if (!Variables[section].ContainsKey(setting))
                Variables[section].Add(setting, value);
            else
                Variables[section][setting] = value;
        }

        private static string GetSection(Section section) {
            switch (section) {
                default: return section.ToString();
            }
        }

        public static bool HasParameter(string setting, Section section) {
            try {
                return ConfigurationManager.AppSettings[GetSection(section) + "." + setting] != null || Variables.ContainsKey(section) &&  Variables[section].ContainsKey(setting);
            } catch (ConfigurationErrorsException e) {
                return false;
            } catch (Exception e) {
                return false;
            }
        }

        public static bool HasParameter<TCheck>(string setting, Section section) {
            if (!typeof(TCheck).IsPrimitive)
                throw new Exception("Unable to check type for " + setting + ". " + typeof(TCheck).Name + " is not a primitive type.");
            try {
                if (GetParameter(setting, section) == null)
                    return false;
                TCheck test = GetParameter<TCheck>(setting, section);
                return true;
            } catch (ConfigurationErrorsException e) {
                return false;
            } catch (ArgumentNullException e) {
                return false;
            } catch (FormatException e) {
                return false;
            } catch (Exception e) {
                return false;
            }
        }

        /// <summary>
        /// Returns the configuration string cast to one of the built in primitive types. If no parameter exists then for every primitive type but bool this method will thrown an exception.
        /// For bool this method will return false.
        /// </summary>
        /// <typeparam name="TReturn">The type to cast the configuration parameter to.</typeparam>
        /// <param name="setting">The name of the setting to look up.</param>
        /// <param name="section">The section the setting is under.</param>
        /// <returns>The setting stored in the configuration parameter 'section.setting' cast to a TReturn.</returns>
        public static TReturn GetParameter<TReturn>(string setting, Section section) {
            Type returnType = typeof(TReturn);
            if (!returnType.IsPrimitive)
                throw new ArgumentException("Specified return type must be a primitive type.");

            string val = GetParameter(setting, section);
            if (val == null)
                return (TReturn)Convert.ChangeType("false", returnType);            
            return (TReturn)Convert.ChangeType(val, returnType);
        }

        public static string GetParameter(string setting, Section section) {
            return Variables.ContainsKey(section) && Variables[section].ContainsKey(setting) ? 
                Variables[section][setting] : 
                ConfigurationManager.AppSettings[GetSection(section) + "." + setting];
        }
    }
}