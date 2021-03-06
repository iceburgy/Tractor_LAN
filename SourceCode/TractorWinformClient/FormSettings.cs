﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace Duan.Xiugang.Tractor
{
    public delegate void SettingsUpdatedEventHandler(bool needRestart);
    public delegate void SettingsSoundVolumeUpdatedEventHandler(int volume);

    public partial class FormSettings : Form
    {
        public event SettingsUpdatedEventHandler SettingsUpdatedEvent;
        public event SettingsSoundVolumeUpdatedEventHandler SettingsSoundVolumeUpdatedEvent;
        public static string regexHostAndPort = @"net.tcp://(?<host>.*):(?<port>\d*)/";

        private static string NodePathEndpoint = "//configuration//system.serviceModel//client//endpoint";
        public static string KeyNickName = "nickName";
        public static string KeyUpdateOnLoad = "updateOnLoad";
        public static string KeyEnableSound = "enableSound";
        public static string KeySoundVolume = "soundVolume";
        public static string KeyShowSuitSeq = "showSuitSeq";
        public static string KeyIsHelpSeen = "isHelpSeen2";
        public static string KeyFullDebug = "fullDebug";
        public static string[] initToTrue = new string[] { KeyEnableSound, KeyShowSuitSeq };

        string hostName = "";

        public FormSettings()
        {
            InitializeComponent();
            hostName = GetHostAndPortFromConfig();

            this.tbxHostName.Text = hostName;
            this.tbxNickName.Text = GetSettingString(KeyNickName);
            this.cbxUpdateOnLoad.Checked = GetSettingBool(KeyUpdateOnLoad);
            this.cbxEnableSound.Checked = GetSettingBool(KeyEnableSound);
            this.tbrGameSoundVolume.Value = Int32.Parse(GetSettingString(KeySoundVolume));
            this.cbxShowSuitSeq.Checked = GetSettingBool(KeyShowSuitSeq);
        }

        public static string GetHostAndPortFromConfig()
        {
            string curHostNameRaw = GetHostName();

            Regex r = new Regex(regexHostAndPort);
            Match m = r.Match(curHostNameRaw);
            if (!m.Success) throw new FormatException();
            return m.Result("${host}:${port}");
        }

        public static string GetHostName()
        {
            return loadConfigDocument().SelectSingleNode(NodePathEndpoint).Attributes["address"].Value;
        }

        public void SaveHostName(string value)
        {
            // load config document for current assembly
            XmlDocument doc = loadConfigDocument();

            // retrieve appSettings node
            XmlNode node = doc.SelectSingleNode(NodePathEndpoint);

            if (node == null)
                throw new InvalidOperationException("Error. Could not find endpoint node in config file.");

            try
            {
                // select the 'add' element that contains the key
                //XmlElement elem = (XmlElement)node.SelectSingleNode(string.Format("//add[@key='{0}']", key));
                node.Attributes["address"].Value = value;

                doc.Save(getConfigFilePath());
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static XmlDocument loadConfigDocument()
        {
            XmlDocument doc = null;
            try
            {
                doc = new XmlDocument();
                doc.Load(getConfigFilePath());
                return doc;
            }
            catch (System.IO.FileNotFoundException e)
            {
                throw new Exception("No configuration file found.", e);
            }
        }

        private static string getConfigFilePath()
        {
            return Assembly.GetExecutingAssembly().Location + ".config";
        }

        public static string GetSettingString(string key)
        {
            try
            {
                AppSettingsReader myreader = new AppSettingsReader();
                return (String)myreader.GetValue(key, typeof(String));
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("does not exist in the appSettings configuration section"))
                {
                    if (key == KeySoundVolume) return "5";
                }
                return "";
            }
        }

        public static bool GetSettingBool(string key)
        {
            try
            {
                AppSettingsReader myreader = new AppSettingsReader();
                return (bool)myreader.GetValue(key, typeof(Boolean));
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("does not exist in the appSettings configuration section") &&
                    initToTrue.Contains(key))
                {
                    SetSetting(key, true.ToString().ToLower());
                    return true;
                }
                return false;
            }
        }

        public static void SetSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configuration.AppSettings.Settings[key] == null)
            {
                configuration.AppSettings.Settings.Add(key, value);
            }
            else
            {
                configuration.AppSettings.Settings[key].Value = value;
            }  
            configuration.Save(ConfigurationSaveMode.Minimal, true);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            bool needRestart = (!string.IsNullOrEmpty(hostName) && !hostName.Equals(this.tbxHostName.Text, StringComparison.InvariantCultureIgnoreCase));
            SaveHostName(string.Format("net.tcp://{0}/TractorHost", this.tbxHostName.Text));
            SetSetting(KeyNickName, tbxNickName.Text);
            SetSetting(KeyUpdateOnLoad, cbxUpdateOnLoad.Checked.ToString().ToLower());
            SetSetting(KeyEnableSound, cbxEnableSound.Checked.ToString().ToLower());
            SetSetting(KeySoundVolume, tbrGameSoundVolume.Value.ToString().ToLower());
            SetSetting(KeyShowSuitSeq, cbxShowSuitSeq.Checked.ToString().ToLower());
            SettingsUpdatedEvent(needRestart);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SettingsUpdatedEvent(false);
            this.Close();
        }

        private void tbrGameSoundVolume_Scroll(object sender, EventArgs e)
        {
            SettingsSoundVolumeUpdatedEvent(this.tbrGameSoundVolume.Value);
        }
    }
}
