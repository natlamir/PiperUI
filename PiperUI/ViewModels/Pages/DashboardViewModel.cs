// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.IO;

namespace PiperUI.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private Dictionary<string, dynamic> voiceData; // Your JSON data

        public DashboardViewModel() 
        {
            InitializeData();
            PopulateLanguageComboBox();
        }

        [ObservableProperty]
        private int _counter = 0;

        [ObservableProperty]
        private List<string> _languages = new List<string>();

        [ObservableProperty]
        private string selectedLanguage;

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
        private void PopulateLanguageComboBox()
        {
            foreach (var voiceKey in voiceData.Keys)
            {
                string languageName = voiceData[voiceKey]["language"]["name_native"];
                if (!_languages.Contains(languageName))
                {
                    _languages.Add(languageName);
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void InitializeData()
        {
            try
            {
                string filePath = "voices.json"; // Update the path to the location of your voices.json file

                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);

                    voiceData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonData);

                    if (voiceData == null)
                    {
                        // Handle the case where deserialization fails
                        MessageBox.Show("Error loading JSON data.");
                    }
                }
                else
                {
                    // Handle the case where the file doesn't exist
                    MessageBox.Show("File 'voices.json' not found.");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
    }
}
