// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using PiperUI.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace PiperUI.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            bool rememberSelectedPageItems = Properties.Settings.Default.RememberSelectedPageItems;
            RememberSelectedPageItemsCheckBox.IsChecked = rememberSelectedPageItems;
        }

        private void RememberSelectedPageItemsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberSelectedPageItems = true;
            Properties.Settings.Default.Save();
        }

        private void RememberSelectedPageItemsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.RememberSelectedPageItems = false;
            Properties.Settings.Default.Save();
        }

        private void ClearSettings()
        {
            Properties.Settings.Default.CustomDropDown = 0;
            Properties.Settings.Default.Language = 0;
            Properties.Settings.Default.Voice = 0;
            Properties.Settings.Default.Quality = 0;
            Properties.Settings.Default.PlaybackSpeed = 0;
            Properties.Settings.Default.Prompt = string.Empty;
        }
    }
}
