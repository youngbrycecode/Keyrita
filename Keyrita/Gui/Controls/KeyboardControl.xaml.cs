﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Keyrita.Settings;
using Keyrita.Settings.SettingUtil;
using Keyrita.Util;

namespace Keyrita.Gui.Controls
{
    /// <summary>
    /// Interaction logic for KeyboardControl.xaml
    /// </summary>
    public partial class KeyboardControl : UserControl
    {
        private int keySize = 80;

        public KeyboardControl()
        {
            InitializeComponent();

            // Create the key objects.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for (int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var nextKey = new Key();
                    nextKey.Width = keySize;
                    nextKey.Height = keySize;
                    nextKey.MouseMove += MoveKey;
                    nextKey.MouseDoubleClick += SelectKey;
                    AccessKeyManager.AddAccessKeyPressedHandler(nextKey, SelectKey);

                    mKeyCanvas.Children.Add(nextKey);
                }
            }

            // Listen to changes in the keyboard offset.
            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                RowOffsets[i] = SettingState.MeasurementSettings.RowOffsets[i];
                RowOffsets[i].LimitsChangedNotifications.AddGui(SyncWithShape);
                RowOffsets[i].LimitsChangedNotifications.AddGui(SyncWithShape);
            }
        }

        #region Key selection

        protected void SelectKey(object sender, EventArgs args)
        {
            Key key = sender as Key;

            if(key != null)
            {
                SettingState.KeyboardSettings.SelectedKey.SetSelection(key.KeyCharacter.Character);
            }
            else
            {
                LTrace.Assert(false, "Select key event triggered without a key.");
            }
        }

        #endregion

        #region Drag and Drop

        class StartDragDropData
        {
            public StartDragDropData(Key clickedKey, Point clickOffset, Point startPosition)
            {
                ClickedKey = clickedKey;
                ClickedOffset = clickOffset;
                StartPosition = startPosition;
            }

            public Key ClickedKey;
            public Point ClickedOffset;
            public Point StartPosition;
        }

        protected void MoveKey(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                StartDragDropData data = new StartDragDropData((Key)sender,
                                                                e.GetPosition((UIElement)sender),
                                                                e.GetPosition(mKeyCanvas));
                Panel.SetZIndex((UIElement)sender, 10000);
                ((UIElement)sender).IsHitTestVisible = false;
                DragDrop.DoDragDrop((DependencyObject) sender, 
                    new DataObject(DataFormats.Serializable, data), 
                    DragDropEffects.Move);
            }
        }

        protected void RepositionKey(object sender, DragEventArgs e)
        {
            var position = e.GetPosition((Canvas)sender);

            StartDragDropData data = (StartDragDropData)e.Data.GetData(DataFormats.Serializable);
            Point offset = data.ClickedOffset;
            Key key = data.ClickedKey;

            Canvas.SetLeft(key, position.X - offset.X);
            Canvas.SetTop(key, position.Y - offset.Y);
        }

        protected void DropKey(object sender, DragEventArgs e)
        {
            // Swap the keys.
            StartDragDropData data = (StartDragDropData)e.Data.GetData(DataFormats.Serializable);
            Point offset = data.ClickedOffset;
            Key key = data.ClickedKey;
            Key draggedOverKey = e.Source as Key;
            Panel.SetZIndex(key, 0);
            key.IsHitTestVisible = true;

            if (draggedOverKey != null && draggedOverKey != key)
            {
                KeyboardState.SwapKeys(key.KeyCharacter.Character, draggedOverKey.KeyCharacter.Character);
            }

            // Reset key to original position.
            Canvas.SetLeft(key, data.StartPosition.X - offset.X);
            Canvas.SetTop(key, data.StartPosition.Y - offset.Y);
        }

        #endregion

        #region Keyboard Sync

        protected void CanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We want to fill the canvas and we know that there are 10.75 keys across.
            // Make sure we don't take too much space vertically as well.
            keySize = (int)(mKeyCanvas.ActualWidth / 10.75);
            double fontSize = (((mKeyCanvas.ActualWidth / 12) / 3 * 2) / 5) * 5.5;

            foreach (Key key in mKeyCanvas.Children)
            {
                key.Width = keySize;
                key.Height = keySize;
                key.mChar.FontSize = fontSize;
            }

            SyncWithShape();
        }

        protected void SyncWithKeyboard(SettingBase settingChanged)
        {
            // Get the key size.
            SyncWithKeyboard();
        }

        protected void SyncWithKeyboard()
        {
            // Go through each of the children and update with the correct key from the kb setting.
            int index = 0;
            foreach (var key in mKeyCanvas.Children)
            {
                int row = index / KeyboardStateSetting.COLS;
                int col = index % KeyboardStateSetting.COLS;
                var k = (Key)key;
                var character = '*';
                var finger = eFinger.None;
                double heatmapValue = 0.0;

                if(mKeyboardState != null)
                {
                    character = mKeyboardState.GetValueAt(row, col);
                }

                if(mKeyMappings != null && ShowFingerUsage != null && ShowFingerUsage.Value.Equals(eOnOff.On))
                {
                    finger = mKeyMappings.GetValueAt(row, col);
                }

                if(mHeatmap != null && mHeatmap.HeatMapData.ContainsKey(character))
                {
                    heatmapValue = mHeatmap.HeatMapData[character];
                }

                k.KeyCharacter = new KeyCharacterWrapper(character, finger, heatmapValue);
                index++;
            }

            SyncWithShape();
        }

        private static readonly DependencyProperty KeyboardProperty =
            DependencyProperty.Register(nameof(KeyboardState),
                                        typeof(KeyboardStateSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnKbSettingChanged));

        private static void OnKbSettingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateKeyboard(e.NewValue as KeyboardStateSetting);
        }

        private void UpdateKeyboard(KeyboardStateSetting newValue)
        {
            // Unregister setting change listeners.
            if (mKeyboardState != null)
            {
                mKeyboardState.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mKeyboardState.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mKeyboardState = newValue;

            if(mKeyboardState != null)
            {
                mKeyboardState.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mKeyboardState.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithKeyboard();
            }
        }

        public KeyboardStateSetting KeyboardState
        {
            get
            {
                return mKeyboardState;
            }
            set
            {
                SetValue(KeyboardProperty, value);
            }
        }

        protected KeyboardStateSetting mKeyboardState;

        #endregion

        #region Layout Sync

        protected void SyncWithShape(SettingBase settingChanged)
        {
            SyncWithShape();
        }

        protected void SyncWithShape()
        {
            // Add offsets to each key depending on their layer.
            int keyIndex = 0;
            var minOffset = 1.0;
            var maxOffset = -1.0;

            foreach(var offset in RowOffsets)
            {
                if((double)offset.Value < minOffset)
                {
                    minOffset = (double)offset.Value;
                }
                else if((double)offset.Value > maxOffset)
                {
                    maxOffset = (double)offset.Value;
                }
            }

            mKeyCanvas.Height = (keySize) * KeyboardStateSetting.ROWS;

            var offsetDifference = maxOffset - minOffset;

            for(int i = 0; i < KeyboardStateSetting.ROWS; i++)
            {
                for(int j = 0; j < KeyboardStateSetting.COLS; j++)
                {
                    var nextKey = mKeyCanvas.Children[keyIndex++];

                    Canvas.SetTop(nextKey, i * (keySize));
                    Canvas.SetLeft(nextKey, j * (keySize) + (double)RowOffsets[i].Value * (double)keySize - (minOffset * keySize));
                }
            }
        }

        /// <summary>
        /// Stores the offsets per row.
        /// </summary>
        protected ConcreteValueSetting<double>[] RowOffsets { get; } = 
            new ConcreteValueSetting<double>[KeyboardStateSetting.ROWS];

        #endregion

        #region Sync With Keys Per Finger

        private static readonly DependencyProperty KeyMappingsProperty =
            DependencyProperty.Register(nameof(KeyMappings),
                                        typeof(KeyMappingSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnKeyMappingChanged));

        private static void OnKeyMappingChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateKeyMappings(e.NewValue as KeyMappingSetting);
        }

        private void UpdateKeyMappings(KeyMappingSetting newValue)
        {
            // Unregister setting change listeners.
            if (mKeyMappings != null)
            {
                mKeyMappings.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mKeyMappings.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mKeyMappings = newValue;

            if(mKeyMappings != null)
            {
                mKeyMappings.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mKeyMappings.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithKeyboard();
            }
        }

        public KeyMappingSetting KeyMappings
        {
            get
            {
                return mKeyMappings;
            }
            set
            {
                SetValue(KeyMappingsProperty, value);
            }
        }

        protected KeyMappingSetting mKeyMappings;

        #endregion

        #region Most Frequent Keys

        private static readonly DependencyProperty HeatmapDataProperty =
            DependencyProperty.Register(nameof(KeyHeatMap),
                                        typeof(HeatmapDataSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnHeatmapDataChanged));

        private static void OnHeatmapDataChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateCharFrequency(e.NewValue as HeatmapDataSetting);
        }

        private void UpdateCharFrequency(HeatmapDataSetting newValue)
        {
            // Unregister setting change listeners.
            if (mHeatmap != null)
            {
                mHeatmap.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mHeatmap.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mHeatmap = newValue;

            if(mHeatmap != null)
            {
                mHeatmap.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mHeatmap.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithKeyboard();
            }
        }

        public HeatmapDataSetting KeyHeatMap
        {
            get
            {
                return mHeatmap;
            }
            set
            {
                SetValue(HeatmapDataProperty, value);
            }
        }

        protected HeatmapDataSetting mHeatmap;

        #endregion

        #region Show Finger Usage

        private static readonly DependencyProperty ShowFingerUsageProperty =
            DependencyProperty.Register(nameof(ShowFingerUsage),
                                        typeof(OnOffSetting),
                                        typeof(KeyboardControl),
                                        new PropertyMetadata(OnShowFingerUsageChanged));

        private static void OnShowFingerUsageChanged(DependencyObject source,
                                             DependencyPropertyChangedEventArgs e)
        {
            var control = source as KeyboardControl;
            control.UpdateFingerUsage(e.NewValue as OnOffSetting);
        }

        private void UpdateFingerUsage(OnOffSetting newValue)
        {
            // Unregister setting change listeners.
            if (mShowFingerUsage != null)
            {
                mShowFingerUsage.ValueChangedNotifications.Remove(SyncWithKeyboard);
                mShowFingerUsage.LimitsChangedNotifications.Remove(SyncWithKeyboard);
            }

            mShowFingerUsage = newValue;

            if(mShowFingerUsage != null)
            {
                mShowFingerUsage.ValueChangedNotifications.AddGui(SyncWithKeyboard);
                mShowFingerUsage.LimitsChangedNotifications.AddGui(SyncWithKeyboard);
                SyncWithKeyboard();
            }
        }

        public OnOffSetting ShowFingerUsage
        {
            get
            {
                return mShowFingerUsage;
            }
            set
            {
                SetValue(ShowFingerUsageProperty, value);
            }
        }

        protected OnOffSetting mShowFingerUsage;

        #endregion
    }
}
