﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using R2API;
using RiskOfOptions.Interfaces;
using RiskOfOptions.OptionComponents;
using RiskOfOptions.OptionOverrides;
using RiskOfOptions.Structs;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;

namespace RiskOfOptions.Options
{
    public class DropDownOption : RiskOfOption, IIntProvider
    {
        public List<UnityAction<int>> Events { get; set; } = new List<UnityAction<int>>();

        private int _value;

        private string[] _choices;

        internal DropDownOption(string modGuid, string modName, string name, object[] description, string defaultValue, string categoryName, string[] choices, bool visibility, bool restartRequired, UnityAction<int> unityAction, bool invokeValueChangedEvent)
            : base(modGuid, modName, name, description, defaultValue, categoryName, null, visibility, restartRequired, invokeValueChangedEvent)
        {
            Value = (int) int.Parse(defaultValue, CultureInfo.InvariantCulture);

            if (unityAction != null)
            {
                Events.Add(unityAction);
            }

            _choices = choices;

            OptionToken = $"{ModSettingsManager.StartingText}.{modGuid}.{modName}.category_{categoryName.Replace(".", "")}.{name}.dropdown".ToUpper().Replace(" ", "_");

            RegisterTokens();
            RegisterChoiceTokens();
        }

        private void RegisterChoiceTokens()
        {
            foreach (var choice in _choices)
            {
                string token = $"{OptionToken}_choice-{choice}".ToUpper().Replace(" ", "_");
                LanguageAPI.Add(token, choice);
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;

                _value = value;

                InvokeListeners();
            }
        }

        public override GameObject CreateOptionGameObject(RiskOfOption option, GameObject prefab, Transform parent)
        {
            GameObject button = GameObject.Instantiate(prefab, parent);

            var dropDownController = button.GetComponentInChildren<DropDownController>();

            dropDownController.choices = _choices;

            dropDownController.settingName = option.ConsoleToken;
            dropDownController.nameToken = option.NameToken;

            button.name = $"Mod Option Drop Down, {option.Name}";

            return button;
        }

        public override void InvokeListeners()
        {
            foreach (var action in Events)
            {
                action.Invoke(Value);
            }
        }

        public void InvokeListeners(int value)
        {
            foreach (var action in Events)
            {
                action.Invoke(value);
            }
        }

        public override string GetValueAsString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override string GetInternalValueAsString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        public override void SetValue(string newValue)
        {
            Value = int.Parse(newValue, CultureInfo.InvariantCulture);
        }

        public override T GetValue<T>()
        {
            if (typeof(T) != typeof(int))
            {
                throw new Exception($"{Name} can only return a int! {typeof(T)} is not a valid return type!");
            }

            return (T)Convert.ChangeType(Value, typeof(T));
        }
    }
}