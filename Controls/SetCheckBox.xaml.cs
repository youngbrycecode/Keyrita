﻿using Keyrita.Settings;
using Keyrita.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Keyrita
{
    /// <summary>
    /// Interaction logic for SetCheckBox.xaml
    /// </summary>
    public partial class SetCheckBox : UserControl
    {
        public SetCheckBox()
        {
            InitializeComponent();

            Setting = SettingState.MeasurementSettings.ShowAnnotations as OnOffSetting;
            LTrace.Assert(Setting != null);
        }

        private void SettingUpdated(SettingBase changedSetting)
        {
            SyncWithSetting();
        }

        private void SyncWithSetting()
        {
            mCheckBox.Checked -= mCheckBox_Checked;
            mCheckBox.Unchecked -= mCheckBox_Unchecked;
            mCheckBox.IsChecked = Setting.IsOn;
            mCheckBox.Checked += mCheckBox_Checked;
            mCheckBox.Unchecked += mCheckBox_Unchecked;

            mCheckBox.IsEnabled = Setting.ValidTokens.Count > 1;
        }

        private void mCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            mSetting.Set(eOnOff.On);
        }

        private void mCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            mSetting.Set(eOnOff.Off);
        }

        /// <summary>
        /// The setting which this control is linked to.
        /// </summary>
        public OnOffSetting Setting
        {
            get
            {
                return mSetting;
            }
            set
            {
                mSetting = value;
                mSetting.ValueChangedNotifications.AddGui(SettingUpdated);
                mSetting.LimitsChangedNotifications.AddGui(SettingUpdated);

                mSettingName.Text = Setting.SettingName;
                SyncWithSetting();
            }
        }

        private OnOffSetting mSetting;
    }
}
