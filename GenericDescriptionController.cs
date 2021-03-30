﻿using R2API;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RiskOfOptions
{
    class GenericDescriptionController : MonoBehaviour
    {
        private GameObject _genericDescriptionPanel;
        public ModOptionPanelController Mopc { get; internal set; }
        private Transform _canvas;

        private void OnEnable()
        {
            if (!_genericDescriptionPanel)
                _genericDescriptionPanel = GameObject.Find("GenericDescriptionPanel");

            _genericDescriptionPanel.SetActive(false);
        }

        private void OnDisable()
        {
            try
            {
                if (!Mopc)
                    Mopc = GetComponentInParent<ModOptionPanelController>();

                if (!Mopc.initilized)
                {
                    return;
                }

                if (_genericDescriptionPanel)
                {
                    _genericDescriptionPanel.SetActive(true);
                }

                if (!_canvas)
                    _canvas = _genericDescriptionPanel.transform.parent.Find("SettingsSubPanel, Mod Options(Clone)");

                Mopc.UnLoad(_canvas);
            }
            catch
            {
                // ignored
            }
        }
    }
}
