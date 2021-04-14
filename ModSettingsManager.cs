﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using EntityStates.AncientWispMonster;
using On.RoR2;
using On.RoR2.UI;
using R2API;
using R2API.Utils;
using RiskOfOptions.OptionOverrides;
using RoR2.ConVar;
using UnityEngine;

using static RiskOfOptions.ExtensionMethods;
using ConCommandArgs = RoR2.ConCommandArgs;

#pragma warning disable 618

namespace RiskOfOptions
{
    public static class ModSettingsManager
    {
        internal static List<OptionContainer> OptionContainers = new List<OptionContainer>();

        private static List<UnityEngine.Events.UnityAction> Listeners = new List<UnityEngine.Events.UnityAction>();

        internal static readonly string StartingText = "risk_of_options";

        internal static bool doingKeybind = false;

        private static bool _initilized = false;


        public static void Init()
        {
            On.RoR2.Console.Awake += AddToConsoleAwake;

            LoadAssets();

            Thunderstore.Init();

            BaseSettingsControlOverride.Init();

            SettingsMenu.Init();

            On.RoR2.PauseManager.CCTogglePause += PauseManagerOnCCTogglePause;
        }

        private static void LoadAssets()
        {
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"RiskOfOptions.Resources.riskofoptions"))
            {
                var MainAssetBundle = AssetBundle.LoadFromStream(assetStream);

                ResourcesAPI.AddProvider(new AssetBundleResourcesProvider($"@RiskOfOptions", MainAssetBundle));
            }
        }

        private static void PauseManagerOnCCTogglePause(PauseManager.orig_CCTogglePause orig, ConCommandArgs args)
        {
            if (doingKeybind)
                return;

            orig(args);
        }

        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static void addStartupListener(UnityEngine.Events.UnityAction unityAction)
        {
            Listeners.Add(unityAction);
        }

        public static void AddListener(UnityEngine.Events.UnityAction<bool> unityAction, string name, string categoryName = "Main", bool restartRequired = false)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();
            Indexes indexes = OptionContainers.GetIndexes(modInfo.ModGuid, name, categoryName);

            OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer].OnValueChangedBool = unityAction;
        }

        public static void AddListener(UnityEngine.Events.UnityAction<float> unityAction, string name, string categoryName = "Main", bool restartRequired = false)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();
            Indexes indexes = OptionContainers.GetIndexes(modInfo.ModGuid, name, categoryName);

            OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer].OnValueChangedFloat = unityAction;
        }

        public static void AddListener(UnityEngine.Events.UnityAction<KeyCode> unityAction, string name, string categoryName = "Main", bool restartRequired = false)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();
            Indexes indexes = OptionContainers.GetIndexes(modInfo.ModGuid, name, categoryName);

            OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer].OnValueChangedKeyCode = unityAction;
        }

        public static RiskOfOption GetOption(string name, string categoryName = "Main")
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();
            Indexes indexes = OptionContainers.GetIndexes(modInfo.ModGuid, name, categoryName);

            return OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer];
        }

        internal static RiskOfOption GetOption(string name, string categoryName, string modGuid)
        {
            Indexes indexes = OptionContainers.GetIndexes(modGuid, name, categoryName);

            return OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer];
        }

        private static void AddToConsoleAwake(On.RoR2.Console.orig_Awake orig, RoR2.Console self)
        {
            orig(self);

            foreach (var mo in OptionContainers.SelectMany(container => container.GetModOptionsCached()))
            {
                mo.ConVar.SetString(mo.ConVar.defaultValue);

                RoR2.Console.instance.InvokeMethod("RegisterConVarInternal", new object[] { mo.ConVar });
                Debug.Log($"{mo.ConVar.name} Option registered to console.");
            }

            RoR2.Console.instance.SubmitCmd(null, "exec config", false);

            foreach (var mo in OptionContainers.SelectMany(container => container.GetModOptionsCached()))
            {
                mo.Value = RoR2.Console.instance.FindConVar(mo.ConsoleToken).GetString();
            }

            Debug.Log($"Invoke Startup Listeners");
            foreach (var item in Listeners)
            {
                item.Invoke();
            }

            _initilized = true;
        }

        // ReSharper disable once InconsistentNaming
        public static void setPanelDescription(string description)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            OptionContainers[OptionContainers.GetContainerIndex(modInfo.ModGuid, modInfo.ModName, true)].Description = description;
        }

        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static void setPanelTitle(string title)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            OptionContainers[OptionContainers.GetContainerIndex(modInfo.ModGuid, modInfo.ModName, true)].Title = title;
        }

        public static void SetModIcon(Sprite iconSprite)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            Thunderstore.AddIcon(modInfo.ModGuid, iconSprite);
        }

        public static void SetVisibility(string name, string categoryName, bool visibility)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();
            Indexes indexes = OptionContainers.GetIndexes(modInfo.ModGuid, name, categoryName);

            OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer].Visibility = visibility;
        }

        public static void RegisterOption(RiskOfOption mo)
        {
            switch (mo.optionType)
            {
                case RiskOfOption.OptionType.Slider:
                    mo.ConVar = new FloatConVar(mo.ConsoleToken, RoR2.ConVarFlags.Archive, mo.DefaultValue, mo.Description);
                    break;
                case RiskOfOption.OptionType.Bool:
                    mo.ConVar = new BoolConVar(mo.ConsoleToken, RoR2.ConVarFlags.Archive, mo.DefaultValue, mo.Description);
                    break;
                case RiskOfOption.OptionType.Keybinding:
                    mo.ConVar = new KeyConVar(mo.ConsoleToken, RoR2.ConVarFlags.Archive, mo.DefaultValue, mo.Description);
                    break;
            }

            if (mo.CategoryName == "Main")
            {
                CreateCategory(mo.CategoryName, "The Main Category", mo.ModGuid, mo.ModName);
            }

            OptionContainers.Add(ref mo);
        }

        public static void AddCheckBox(string name, string description, bool defaultValue, string categoryName, CheckBoxOverride checkBoxOverride, bool visibility = true, bool restartRequired = false)
        {
            if (_initilized)
                throw new Exception($"AddCheckBox {name}, under Category {categoryName}, was called after initilization of RiskOfOptions. \n This usually means you are calling this outside of Awake()");

            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            RegisterOption(new RiskOfOption(modInfo.ModGuid, modInfo.ModName, RiskOfOption.OptionType.Bool, name, description, $"{(defaultValue ? "1" : "0")}", categoryName, checkBoxOverride, visibility, restartRequired));
        }

        public static void AddCheckBox(string name, string description, bool defaultValue, string categoryName, bool visibility = true, bool restartRequired = false)
        {
            if (_initilized)
                throw new Exception($"AddCheckBox {name}, under Category {categoryName}, was called after initilization of RiskOfOptions. \n This usually means you are calling this outside of Awake()");

            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            RegisterOption(new RiskOfOption(modInfo.ModGuid, modInfo.ModName, RiskOfOption.OptionType.Bool, name, description, $"{(defaultValue ? "1" : "0")}", categoryName, null, visibility, restartRequired));
        }

        public static void AddSlider(string name, string description, float defaultValue, string categoryName, SliderOverride sliderOverride, bool visibility = true, bool restartRequired = false)
        {
            if (_initilized)
                throw new Exception($"AddSlider {name}, under Category {categoryName}, was called after initilization of RiskOfOptions. \n This usually means you are calling this outside of Awake()");

            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            RegisterOption(new RiskOfOption(modInfo.ModGuid, modInfo.ModName, RiskOfOption.OptionType.Slider, name, description, defaultValue.ToString(CultureInfo.InvariantCulture), categoryName, sliderOverride, visibility, restartRequired));
        }

        public static void AddSlider(string name, string description, float defaultValue, string categoryName, bool visibility = true, bool restartRequired = false)
        {
            if (_initilized)
                throw new Exception($"AddSlider {name}, under Category {categoryName}, was called after initilization of RiskOfOptions. \n This usually means you are calling this outside of Awake()");

            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            RegisterOption(new RiskOfOption(modInfo.ModGuid, modInfo.ModName, RiskOfOption.OptionType.Slider, name, description, defaultValue.ToString(CultureInfo.InvariantCulture), categoryName, null, visibility, restartRequired));
        }

        public static void AddKeyBind(string name, string description, KeyCode defaultValue, string categoryName, bool visibility = true)
        {
            if (_initilized)
                throw new Exception($"AddKeyBind {name}, under Category {categoryName}, was called after initilization of RiskOfOptions. \n This usually means you are calling this outside of Awake()");

            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            RegisterOption(new RiskOfOption(modInfo.ModGuid, modInfo.ModName, RiskOfOption.OptionType.Keybinding, name, description, $"{(int)defaultValue}", categoryName, null, visibility, false));
        }

        public static void CreateCategory(string name)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            if (!OptionContainers.Contains(modInfo.ModGuid))
                OptionContainers.Add(new OptionContainer(modInfo.ModGuid, modInfo.ModName));


            for (int i = 0; i < OptionContainers[OptionContainers.GetContainerIndex(modInfo.ModGuid, modInfo.ModName)].GetCategoriesCached().Count; i++)
            {
                if (OptionContainers[OptionContainers.GetContainerIndex(modInfo.ModGuid, modInfo.ModName)].GetCategoriesCached()[i].Name == name)
                {
                    Debug.Log($"Category {name} already exists!, please make sure you aren't assigning a category before creating one, or you aren't creating the same category twice!", BepInEx.Logging.LogLevel.Warning);
                    return;
                }
            }

            OptionCategory newCategory = new OptionCategory(name, modInfo.ModGuid);

            OptionContainers[OptionContainers.GetContainerIndex(modInfo.ModGuid, modInfo.ModName)].Add(ref newCategory);
        }


        internal static void CreateCategory(string name, string description, string modGuid, string modName)
        {
            if (!OptionContainers.Contains(modGuid))
                OptionContainers.Add(new OptionContainer(modGuid, modName));

            for (int i = 0; i < OptionContainers[OptionContainers.GetContainerIndex(modGuid, modName)].GetCategoriesCached().Count; i++)
            {
                if (OptionContainers[OptionContainers.GetContainerIndex(modGuid, modName)].GetCategoriesCached()[i].Name == name)
                {
                    //Debug.Log($"Category {Name} already exists!, please make sure you aren't assigning a category before creating one, or you aren't creating the same category twice!", BepInEx.Logging.LogLevel.Warning);
                    return;
                }
            }

            OptionCategory newCategory = new OptionCategory(name, modGuid)
            {
                Description = description
            };


            OptionContainers[OptionContainers.GetContainerIndex(modGuid, modName)].Insert(ref newCategory);
        }

        internal static RiskOfOption GetOption(string consoleToken)
        {
            foreach (OptionContainer container in OptionContainers)
            {
                for (int i = 0; i < container.GetModOptionsCached().Count; i++)
                {
                    if (container.GetModOptionsCached()[i].ConsoleToken == consoleToken)
                    {
                        return container.GetModOptionsCached()[i];
                    }
                }
            }

            throw new Exception($"An ROO couldn't be found for {consoleToken}!");
        }

        internal static Thunderstore.ModSearchEntry[] GetIconSearchEntries()
        {
            List<Thunderstore.ModSearchEntry> modSearchEntries = new List<Thunderstore.ModSearchEntry>();

            foreach (var container in OptionContainers)
            {
                modSearchEntries.Add(new Thunderstore.ModSearchEntry()
                {
                    fullName = $"{container.ModGuid.Split('.')[1]}-{container.ModGuid.Split('.')[2]}",
                    fullNameWithUnderscores = $"{container.ModGuid.Split('.')[1]}-{container.ModName.Replace(" ", "_")}",
                    fullNameWithoutSpaces = $"{container.ModGuid.Split('.')[1]}-{container.ModName.Replace(" ", "")}",
                    nameWithUnderscores = $"{container.ModName.Replace(" ", "_")}",
                    nameWithoutSpaces = $"{container.ModName.Replace(" ", "")}",
                    modGuid = container.ModGuid,
                    modName = container.ModName
                });
                //Debug.Log($"Search terms for {container.ModGuid} are:" +
                //          $"\n {modSearchEntries[modSearchEntries.Count - 1].fullName}" +
                //          $"\n {modSearchEntries[modSearchEntries.Count - 1].fullNameWithUnderscores}" +
                //          $"\n {modSearchEntries[modSearchEntries.Count - 1].fullNameWithoutSpaces}" +
                //          $"\n {modSearchEntries[modSearchEntries.Count - 1].nameWithUnderscores}" +
                //          $"\n {modSearchEntries[modSearchEntries.Count - 1].nameWithoutSpaces}");
            }

            return modSearchEntries.ToArray();
        }

        //internal struct OptionOverrideInfo
        //{
        //    internal string name;
        //    internal string categoryName;
        //    internal string modGuid;
        //}

        #region ModOption Legacy Stuff

        [Obsolete("Usage of ModOption is depreciated, use RiskOfOption instead.")]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static void addListener(ModOption modOption, UnityEngine.Events.UnityAction<float> unityAction)
        {
            Indexes indexes = OptionContainers.GetIndexes(modOption.owner, modOption.name);

            OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer].OnValueChangedFloat = unityAction;
        }

        [Obsolete("Usage of ModOption is depreciated, use RiskOfOption instead.")]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static void addListener(ModOption modOption, UnityEngine.Events.UnityAction<bool> unityAction)
        {
            Indexes indexes = OptionContainers.GetIndexes(modOption.owner, modOption.name);

            OptionContainers[indexes.ContainerIndex].GetModOptionsCached()[indexes.OptionIndexInContainer].OnValueChangedBool = unityAction;
        }

        [Obsolete("ModOptions are handled internally now. Please use AddCheckBox, AddSlider, etc", false)]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static void addOption(ModOption mo)
        {
            Debug.Log($"Legacy ModOption {mo.name} constructed, converting to RiskOfOption...");

            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            RegisterOption(new RiskOfOption(modInfo.ModGuid, modInfo.ModName, (RiskOfOption.OptionType)mo.optionType, mo.name, mo.description, mo.defaultValue, "", null, true, true));
        }

        [Obsolete("ModOption is obsolete, please use RiskOfOption instead.")]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static ModOption getOption(string name)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            foreach (var item in OptionContainers[OptionContainers.GetContainerIndex(modInfo.ModGuid, modInfo.ModName)].GetModOptionsCached())
            {
                if (!string.Equals(item.Name, name, StringComparison.InvariantCultureIgnoreCase)) continue;

                var temp = new ModOption((ModOption.OptionType) item.optionType, item.Name, item.Description, item.DefaultValue)
                {
                    conVar = item.ConVar
                };

                temp.SetOwner(modInfo.ModGuid);

                return temp;
            }

            Debug.Log($"ModOption {name} not found!", BepInEx.Logging.LogLevel.Error);
            return null;
        }


        //[Obsolete("Use GetOption(Name, Category).GetBool() / .GetFloat() / .GetKeyCode() instead")]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static string getOptionValue(string name)
        {
            ModInfo modInfo = Assembly.GetCallingAssembly().GetExportedTypes().GetModInfo();

            BaseConVar conVar = (from item in OptionContainers[OptionContainers.GetContainerIndex(modInfo.ModGuid, modInfo.ModName)].GetModOptionsCached() where string.Equals(item.Name, name, StringComparison.InvariantCultureIgnoreCase) select RoR2.Console.instance.FindConVar(item.ConsoleToken)).FirstOrDefault();

            if (conVar != null)
                return conVar.GetString();

            Debug.Log($"Convar {name} not found in convars.", BepInEx.Logging.LogLevel.Error);
            return "";
        }
        #endregion
    }
}
