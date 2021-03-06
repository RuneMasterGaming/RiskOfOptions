﻿using BepInEx;
using RoR2.ConVar;
using System.Reflection;
using UnityEngine;

namespace RiskOfOptions
{
    public class ModOption
    {
        public OptionType optionType;
        public string longName;
        public string owner { get; private set; }
        public string modName { get; private set; }
        public string name;
        public string description;

        public string defaultValue;

        public UnityEngine.Events.UnityAction<bool> onValueChangedBool;
        public UnityEngine.Events.UnityAction<float> onValueChangedFloat;

        public BaseConVar conVar;

        public GameObject gameObject;

        public enum OptionType
        {
            Bool,
            Slider,
            Keybinding
        }

        public ModOption(OptionType _optionType, string _name, string _description, string _defaultValue = null)
        {
            optionType = _optionType;
            name = _name;
            description = _description;
            defaultValue = _defaultValue;

            var classes = Assembly.GetCallingAssembly().GetExportedTypes();

            foreach (var item in classes)
            {
                BepInPlugin bepInPlugin = item.GetCustomAttribute<BepInPlugin>();

                if (bepInPlugin != null)
                {
                    owner = bepInPlugin.GUID;
                    modName = bepInPlugin.Name;
                }
            }
        }

        public static bool operator ==(ModOption a, ModOption b)
        {
            if (a.longName == b.longName)
            {
                return true;
            }
            return false;
        }

        public static bool operator !=(ModOption a, ModOption b)
        {
            if (a.longName != b.longName)
            {
                return true;
            }
            return false;
        }


    }
}
