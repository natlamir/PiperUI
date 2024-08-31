// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using PiperUI.ViewModels.Pages;
using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace PiperUI.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        private Dictionary<string, dynamic> voiceData; // Your JSON data
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            InitializeData(); // Load your JSON data into voiceData
            PopulateLanguageComboBox();
            PopulateCustomComboBox();
        }

        private void pageLoad(object sender, RoutedEventArgs e)
        {
            // This will get the current WORKING directory (i.e. \bin\Debug)
            string workingDirectory = Environment.CurrentDirectory;
            // or: Directory.GetCurrentDirectory() gives the same result

            // This will get the current PROJECT bin directory (ie ../bin/)
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.FullName;

            // This will get the current PROJECT directory
            string projectDirectory2 = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
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
                        System.Windows.MessageBox.Show("Error loading JSON data.");
                    }
                }
                else
                {
                    // Handle the case where the file doesn't exist
                    System.Windows.MessageBox.Show("File 'voices.json' not found.");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void PopulateCustomComboBox()
        {
            // Check if the folder exists
            if (Directory.Exists("custom"))
            {
                // Get all files with the ".onnx" extension in the folder
                string[] onnxFiles = Directory.GetFiles("custom", "*.onnx");

                // Iterate through the files and add their names to the ComboBox
                foreach (string filePath in onnxFiles)
                {
                    // Get the file name without extension
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                    // Add the file name to the ComboBox
                    customComboBox.Items.Add(fileNameWithoutExtension);
                }
            }
        }

        private void PopulateLanguageComboBox()
        {
            foreach (var voiceKey in voiceData.Keys)
            {
                string languageName = voiceData[voiceKey]["language"]["name_native"];
                if (!languageComboBox.Items.Contains(languageName))
                {
                    languageComboBox.Items.Add(languageName);
                }
            }
        }

        private void PopulateVoiceNameComboBox(string selectedLanguage)
        {
            voiceNameComboBox.Items.Clear();

            foreach (var voiceKey in voiceData.Keys)
            {
                string languageName = voiceData[voiceKey]["language"]["name_native"];
                string voiceName = voiceData[voiceKey]["name"];

                if (languageName == selectedLanguage && !voiceNameComboBox.Items.Contains(voiceName))
                {
                    voiceNameComboBox.Items.Add(voiceName);
                }
            }
        }

        private void PopulateQualityComboBox(string selectedVoiceName)
        {
            qualityComboBox.Items.Clear();

            foreach (var voiceKey in voiceData.Keys)
            {
                string voiceName = voiceData[voiceKey]["name"];
                string quality = voiceData[voiceKey]["quality"];

                if (voiceName == selectedVoiceName && !qualityComboBox.Items.Contains(quality))
                {
                    qualityComboBox.Items.Add(quality);
                }
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Language = languageComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
            string selectedLanguage = languageComboBox.SelectedItem as string;
            PopulateVoiceNameComboBox(selectedLanguage);
        }

        private void VoiceNameComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Voice = voiceNameComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
            string selectedVoiceName = voiceNameComboBox.SelectedItem as string;
            PopulateQualityComboBox(selectedVoiceName);
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {            
            string selectedVoiceName = voiceNameComboBox.SelectedItem as string;
            string selectedQuality = qualityComboBox.SelectedItem as string;
            string selectedLanguage = languageComboBox.SelectedItem as string;
            string customVoice = customComboBox.SelectedItem as string;

            if(!string.IsNullOrWhiteSpace(txtPrompt.Text) && customVoice != null)
            {
                await Task.Run(() =>
                {
                    CallPiper("custom", customVoice + ".onnx");
                });
            }
            else if (!string.IsNullOrWhiteSpace(txtPrompt.Text) && selectedLanguage != null && selectedVoiceName != null && selectedQuality != null)
            {
                string countryCode = GetCountryCode(selectedVoiceName, selectedQuality);
                string onnxFile = "";

                if (countryCode != null && voiceData.ContainsKey(countryCode))
                {
                    onnxFile = await DownloadFiles(voiceData[countryCode]["files"]);
                }

                await Task.Run(() =>
                {
                    CallPiper("models", onnxFile);
                });
            }
            else
            {
                System.Windows.MessageBox.Show("Make valid selections and enter a prompt");
            }
        }

        private int GetNextFileNumber(string folderPath)
        {
            // Ensure the folder exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Get all files with the .wav extension
            string[] files = Directory.GetFiles(folderPath, "*.wav");

            // Determine the next file number
            int nextFileNumber = files.Length + 1;

            return nextFileNumber;
        }


        private void CallPiper(string modelFolder, string onnxFile)
        {
            string folderPath = "output"; // Replace with your actual folder path
            int nextFile = GetNextFileNumber(folderPath);

            if (!Directory.Exists("output"))
                Directory.CreateDirectory("output");

            string prompt = "";
            double playbackSpeed = 0;

            this.Dispatcher.Invoke(() =>
            {
                prompt = txtPrompt.Text;
                playbackSpeed = playbackSpeedSlider.Value;
            });

            prompt = CleanString(prompt);

            string command = "chcp 65001 | echo '" + prompt.Replace("'", "''") + "' | piper --model " + modelFolder + "\\" + onnxFile + " --length_scale " + playbackSpeed + " --output_file output\\" + nextFile + ".wav";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{command}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            Process process = new Process { StartInfo = psi };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();
            int exitCode = process.ExitCode;

            process.Close();
            SoundPlayer player = new SoundPlayer("output\\" + nextFile + ".wav");
            player.Load();
            player.Play();
        }

        private string CleanString(string input)
        {
            return input
                .Replace("&", "and")
                .Replace("|", "")
                .Replace("\"", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("\r\n", " ")
                .Replace("\n", " ");
        }

        private string GetCountryCode(string selectedVoiceName, string selectedQuality)
        {
            foreach (var countryCode in voiceData.Keys)
            {
                if (countryCode.Contains(selectedVoiceName + "-" + selectedQuality))
                    return countryCode;
            }

            return null; // Handle the case where the country code is not found
        }

        private async Task<string> DownloadFiles(dynamic files)
        {
            try
            {
                string resultFileName = string.Empty;

                foreach (var file in files)
                {
                    string fileName = file.Name;
                    if (!fileName.EndsWith(".onnx") && !fileName.EndsWith(".json"))
                        continue;

                    string fileUrl = @"https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.0/" + fileName + "?download=true";

                    if (!File.Exists("models\\" + System.IO.Path.GetFileName(fileName)))
                    {
                        lblGenerate.Content = "downloading file: " + fileName;

                        using (WebClient client = new WebClient())
                        {
                            if (!Directory.Exists("models"))
                                Directory.CreateDirectory("models");

                            await Task.Run(() => client.DownloadFile(fileUrl, "models\\" + System.IO.Path.GetFileName(fileName)));

                            if (fileName.EndsWith(".onnx"))
                                resultFileName = System.IO.Path.GetFileName(fileName);
                        }
                    }
                    else
                    {
                        lblGenerate.Content = "";

                        if (fileName.EndsWith(".onnx"))
                            resultFileName = System.IO.Path.GetFileName(fileName);
                    }
                }
                lblGenerate.Content = "";
                return resultFileName;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred during file download: {ex.Message}");
                return string.Empty; // Handle the exception by returning a default value
            }
        }

        private void qualityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Quality = qualityComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void customComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.CustomDropDown = customComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            customComboBox.SelectedIndex = -1;
        }

        private void PlaybackSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Properties.Settings.Default.PlaybackSpeed = playbackSpeedSlider.Value;
            Properties.Settings.Default.Save();

            if (playbackSpeedLabel != null)
            {
                double speed = Math.Round(playbackSpeedSlider.Value, 1);
                string speedDescription = speed == 1 ? "Normal" : speed < 1 ? "Fast" : "Slow";
                playbackSpeedLabel.Text = $"{speedDescription} ({speed:F1}x)";
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if(Properties.Settings.Default.RememberSelectedPageItems)
            {
                customComboBox.SelectedIndex = Properties.Settings.Default.CustomDropDown;
                languageComboBox.SelectedIndex = Properties.Settings.Default.Language;
                voiceNameComboBox.SelectedIndex = Properties.Settings.Default.Voice;
                qualityComboBox.SelectedIndex = Properties.Settings.Default.Quality;
                playbackSpeedSlider.Value = Properties.Settings.Default.PlaybackSpeed;
                txtPrompt.Text = Properties.Settings.Default.Prompt;
            }

            // is there a better way to do detect page closing?            
            this.Dispatcher.ShutdownStarted += AppClosing;
        }

        private void AppClosing(object? sender, EventArgs e)
        {
            if (Properties.Settings.Default.RememberSelectedPageItems)
            {
                Properties.Settings.Default.CustomDropDown = customComboBox.SelectedIndex;
                Properties.Settings.Default.Language = languageComboBox.SelectedIndex;
                Properties.Settings.Default.Voice = voiceNameComboBox.SelectedIndex;
                Properties.Settings.Default.Quality = qualityComboBox.SelectedIndex;
                Properties.Settings.Default.PlaybackSpeed = playbackSpeedSlider.Value;
                Properties.Settings.Default.Prompt = txtPrompt.Text;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.CustomDropDown = -1;
                Properties.Settings.Default.Language = -1;
                Properties.Settings.Default.Voice = -1;
                Properties.Settings.Default.Quality = -1;
                Properties.Settings.Default.PlaybackSpeed = 1;
                Properties.Settings.Default.Prompt = string.Empty;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // why does this not fire?
        }

        private void txtPrompt_LostFocus(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Prompt = txtPrompt.Text;
            Properties.Settings.Default.Save();
        }
    }
}
