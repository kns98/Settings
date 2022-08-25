using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Drawing.Design;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using SettingsDialog.Properties;

namespace SettingsDialog
{
    public partial class SettingsEditForm : Form
    {
        /// <summary>
        //Keep a copy of your settings
        /// </summary>
        private readonly Settings copiedSettings;

        /// <summary>
        //constructor
        /// </summary>
        public SettingsEditForm()
        {
            InitializeComponent();

            // Additions cannot be made in the initial edit screen of the StringCollection.
            //By executing this, the edit screen changes and you can add it.
            TypeDescriptor.AddAttributes(typeof(StringCollection),
                new EditorAttribute(
                    "System.Windows.Forms.Design. StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    typeof(UITypeEditor)));

            //Make a copy of your settings
            copiedSettings = new Settings();
            foreach (SettingsProperty property in Settings.Default.Properties)
                //Required for StringCollection type

                copiedSettings[property.Name] = deepCopy(Settings.Default[property.Name]);

            // Make the copied object visible

            propertyGrid1.SelectedObject = copiedSettings;
        }

        /// <summary>
        ////Make a deep copy of an object
        /// </summary>
        private static T deepCopy<T>(T src)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, src);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }

        /// <summary>
        //Save your edits
        /// </summary>
        /// <param name="sender"></param>
        //<param name="e"></param>
        private void buttonSave_Click(object sender, EventArgs e)
        {
            foreach (SettingsProperty property in Settings.Default.Properties)
                // Required for StringCollection type

                Settings.Default[property.Name] = deepCopy(copiedSettings[property.Name]);
            Settings.Default.Save();
            MessageBox.Show("Saved");
        }

        /// <summary>
        //Export the current saved values
        /// </summary>
        /// <param name="sender"></param>
        //<param name="e"></param>
        private void buttonExport_Click(object sender, EventArgs e)
        {
            //Select File
            string fullPath;
            using (var sfd = new SaveFileDialog())
            {
                sfd.FileName = "user.config";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                sfd.Filter = "configuration file (*.config)|*.config";
                sfd.FilterIndex = 1;
                sfd.Title = "Select the destination file";
                sfd.RestoreDirectory = true;
                if (sfd.ShowDialog() != DialogResult.OK) return;

                fullPath = sfd.FileName;
            }

            //Copy File
            try
            {
                //Get the path of user.config
                var userConfigPath = ConfigurationManager
                    .OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

                //If there is no file, save() and generate it
                if (!File.Exists(userConfigPath)) Settings.Default.Save();

                //Export is just copying files
                File.Copy(userConfigPath, fullPath, true);
                MessageBox.Show("Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Export Failed", MessageBoxButtons.OK);
            }
        }

        /// <summary>
        //Import settings from a file
        /// </summary>
        /// <param name="sender"></param>
        //<param name="e"></param>
        private void buttonImport_Click(object sender, EventArgs e)
        {
            //File Selection
            var fullPath = "";
            using (var ofd = new OpenFileDialog())
            {
                ofd.FileName = "user.config";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                ofd.Filter = "configuration file (*.config)|*.config";
                ofd.FilterIndex = 1;
                ofd.Title = "Please select a file to import";
                ofd.RestoreDirectory = true;
                if (ofd.ShowDialog() != DialogResult.OK) return;

                fullPath = ofd.FileName;
            }

            //Load
            ClientSettingsSection section = null;
            try
            {
                // Even if you specify only the file to be imported into ExeConfigFilename, it cannot be read correctly by GetSection because the section information is not written in that file. 
                //In addition, even if you specify the application settings in ExeConfigFilename and the file to be imported into RoamingUserConfigFilename, it may not work correctly.
                //For example, if there is a new setting that has not been vomited into the file to be imported, you should originally want to keep the current value, but it will be overwritten with the default value.
                //So, specify ExeConfigFilename/ RoamingUserConfigFilenam / LocalUserConfigFilename and load it.

                var tmpAppConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var tmpUserCOnfig =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                var exeFileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = tmpAppConfig.FilePath,
                    RoamingUserConfigFilename = tmpUserCOnfig.FilePath,
                    LocalUserConfigFilename = fullPath
                };
                var config =
                    ConfigurationManager.OpenMappedExeConfiguration(exeFileMap,
                        ConfigurationUserLevel.PerUserRoamingAndLocal);
                section = (ClientSettingsSection)config.GetSection($"userSettings/{typeof(Settings).FullName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Import Failed", MessageBoxButtons.OK);
                return;
            }

            //Refreshing data
            try
            {
                // Key: Create a dictionary of the property name, Value: SettingElement, of the corresponding property of the imported file
                var dict = new Dictionary<string, SettingElement>();
                foreach (SettingElement v in section.Settings) dict.Add(v.Name, v);

                // Update the current settings
                foreach (SettingsPropertyValue value in copiedSettings.PropertyValues)
                {
                    SettingElement element;
                    if (dict.TryGetValue(value.Name, out element))
                    {
                        //If SerializedValue is not referenced even once, it is specified that it will return to the original value when it was referenced.
                        // https://referencesource.microsoft.com/#System/sys/system/configuration/SettingsPropertyValue.cs,69
                        //As a countermeasure, forcibly change the internal member to false by reflection.
                        //Even without reflection, var dummy = value.It may be a method of executing SerializedValue and referencing it once.
                        var _ChangedSinceLastSerialized = typeof(SettingsPropertyValue).GetField(
                            "_ChangedSinceLastSerialized",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Instance);
                        _ChangedSinceLastSerialized.SetValue(value, false);

                        //Setting the value
                        value.SerializedValue = element.Value.ValueXml.InnerXml;

                        //value.If you set Deserialized to false, the value.It is deserialized when the PropertyValue is accessed.
                        // https://referencesource.microsoft.com/#System/sys/system/configuration/SettingsPropertyValue.cs,40
                        value.Deserialized = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Import Failed", MessageBoxButtons.OK);
                return;
            }

            //Refresh Screen
            propertyGrid1.SelectedObject = copiedSettings;

            //message
            MessageBox.Show("Press Save to reflect imported settings");
        }


        private void SettingsEditForm_Load(object sender, EventArgs e)
        {
            
        }

        private void propertyGrid1_Click(object sender, EventArgs e)
        {

        }

        private void buttonClose_Click(object sender, EventArgs e)
        {

        }
    }
}