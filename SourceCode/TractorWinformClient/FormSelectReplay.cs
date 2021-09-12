using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Duan.Xiugang.Tractor
{
    public partial class FormSelectReplay : Form
    {
        string replayRootFolder;
        public FormSelectReplay(string rootFolder)
        {
            InitializeComponent();
            replayRootFolder = rootFolder;
            initReplays();
        }

        private void initReplays()
        {
            DirectoryInfo dirRoot = new DirectoryInfo(replayRootFolder);
            DirectoryInfo[] dirs = dirRoot.GetDirectories();
            for (int i = 0; i < dirs.Length; i++)
            {
                cbbReplayDate.Items.Add(dirs[i].Name);
            }

            if (cbbReplayDate.Items.Count > 0)
            {
                cbbReplayDate.SelectedIndex = 0;
            }
        }

        private void cbbReplayDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbbReplayName.Items.Clear();
            DirectoryInfo selectedDate = new DirectoryInfo(string.Format("{0}\\{1}", replayRootFolder, cbbReplayDate.SelectedItem));
            FileInfo[] files = selectedDate.GetFiles();
            for (int j = 0; j < files.Length; j++)
            {
                cbbReplayName.Items.Add(files[j].Name);
            }

            if (cbbReplayName.Items.Count > 0)
            {
                cbbReplayName.SelectedIndex = 0;
            }
        }
    }
}
