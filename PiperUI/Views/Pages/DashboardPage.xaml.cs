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
            string selectedLanguage = languageComboBox.SelectedItem as string;
            PopulateVoiceNameComboBox(selectedLanguage);
        }

        private void VoiceNameComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            string selectedVoiceName = voiceNameComboBox.SelectedItem as string;
            PopulateQualityComboBox(selectedVoiceName);
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            
            
            string selectedVoiceName = voiceNameComboBox.SelectedItem as string;
            string selectedQuality = qualityComboBox.SelectedItem as string;
            string selectedLanguage = languageComboBox.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(txtPrompt.Text) && selectedLanguage != null && selectedVoiceName != null && selectedQuality != null)
            {
                string countryCode = GetCountryCode(selectedVoiceName, selectedQuality);
                string onnxFile = "";

                if (countryCode != null && voiceData.ContainsKey(countryCode))
                {
                    onnxFile = await DownloadFiles(voiceData[countryCode]["files"]);
                }

                await Task.Run(() =>
                {
                    CallPiper(onnxFile);
                });
            }
            else
            {
                System.Windows.MessageBox.Show("Make all selections and enter a prompt");
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


        private void CallPiper(string onnxFile)
        {
            string folderPath = "output"; // Replace with your actual folder path
            int nextFile = GetNextFileNumber(folderPath);

            if (!Directory.Exists("output"))
                Directory.CreateDirectory("output");

            string prompt = "";

            this.Dispatcher.Invoke(() =>
            {
                prompt = txtPrompt.Text;
            });

            string command = "echo '" + prompt.Replace("'", "''") + "' | " + Environment.CurrentDirectory + "\\piper.exe --model models\\" + onnxFile + " --output_file output\\" + nextFile + ".wav";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-Command \"{command}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                //WindowStyle = ProcessWindowStyle.Hidden,
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

        }
    }
}
