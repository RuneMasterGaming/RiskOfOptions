﻿using System;
using System.Collections.Generic;
using System.Text;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;

namespace RiskOfOptions.OptionComponents
{
    // ReSharper disable once IdentifierTypo
    public class KeybindController : BaseSettingsControl
    {
        private bool _listening = false;
        private TextMeshProUGUI _keybindLabel;

        public UnityEngine.Events.UnityAction<KeyCode> onValueChangedKeyCode;

        protected new void Awake()
        {
            base.settingSource = (BaseSettingsControl.SettingSource) 2;

            if (base.settingName == "")
                return;

            base.Awake();

            _keybindLabel = transform.Find("BindingContainer").Find("BindingButton").Find("ButtonText").GetComponent<TextMeshProUGUI>();
        }

        protected new void Start()
        {
            base.Start();

            SetDisplay();
        }


        public void StartListening()
        {
            _listening = true;
        }

        public void OnGUI()
        {
            if (!_listening)
                return;

            if (!Event.current.isKey || Event.current.type != EventType.KeyDown)
                return;

            if (Event.current.keyCode == KeyCode.None)
                return;


            Debug.Log($"Key {Event.current.keyCode} Pressed!");

            base.SubmitSetting($"{(int)Event.current.keyCode}");

            StopListening();
        }

        protected override void Update()
        {
            base.Update();

            if (!_listening)
                return;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                Debug.Log($"Key {KeyCode.LeftShift} Pressed!");

                base.SubmitSetting($"{(int)KeyCode.LeftShift}");

                StopListening();
            }
            else if (Input.GetKey(KeyCode.RightShift))
            {
                Debug.Log($"Key {KeyCode.RightShift} Pressed!");

                base.SubmitSetting($"{(int)KeyCode.RightShift}");

                StopListening();
            }
        }

        public void StopListening()
        {
            _listening = false;

            SetDisplay();
        }

        private void SetDisplay()
        {
            _keybindLabel.SetText(((KeyCode)int.Parse(base.GetCurrentValue())).ToString());
        }
    }
}