using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SettingsDialog.Properties;

namespace SettingsDialog
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // データグリッドを更新
            dataGridView1.Columns.Add("Name", "Name");
            dataGridView1.Columns.Add("Value", "Value");
            dataGridView1.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Settings.Default.SettingChanging += (s, e) => updateDataGrid();
            updateDataGrid();
        }

        /// <summary>
        ///     プロパティの編集画面を開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOpen_Click(object sender, EventArgs e)
        {
            using (var form = new SettingsEditForm())
            {
                form.ShowDialog();
            }
        }

        /// <summary>
        ///     DataGridを最新の値に更新する
        /// </summary>
        private void updateDataGrid()
        {
            dataGridView1.Rows.Clear();
            foreach (SettingsProperty property in Settings.Default.Properties)
                if (Settings.Default[property.Name] is StringCollection)
                {
                    var collection = (Settings.Default[property.Name] as StringCollection).Cast<string>();
                    dataGridView1.Rows.Add(property.Name, string.Join("\n", collection));
                }
                else
                {
                    dataGridView1.Rows.Add(property.Name, Settings.Default[property.Name].ToString());
                }
        }

        /// <summary>
        ///     user.configのディレクトリを開く。
        ///     存在しない場合があるので、存在するところまでパスを削って開く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOpenDir_Click(object sender, EventArgs e)
        {
            var userConfigPath = ConfigurationManager
                .OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            var directory = Path.GetDirectoryName(userConfigPath);
            while (!string.IsNullOrEmpty(directory))
            {
                if (Directory.Exists(directory))
                {
                    Process.Start(directory);
                    return;
                }

                directory = Path.GetDirectoryName(directory);
            }

            ;
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }
    }
}