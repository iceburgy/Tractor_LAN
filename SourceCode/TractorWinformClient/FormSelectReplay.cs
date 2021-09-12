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

            DirectoryInfo dir = new DirectoryInfo(rootFolder);
            initReplays(dir);
        }

        private void initReplays(DirectoryInfo dir)
        {
            DirectoryInfo[] dis = dir.GetDirectories();
            for (int i = 0; i < dis.Length; i++)
            {
                cbbReplayDate.Items.Add(dis[i].Name);

                string replaySubDirectory = dis[i].FullName;
                FileInfo[] files = dis[i].GetFiles();
                for (int j = 0; j < files.Length; j++)
                {
                    cbbReplayName.Items.Add(files[j].FullName.Substring(replaySubDirectory.Length + 1));
                }
            }

            if (cbbReplayDate.Items.Count > 0)
            {
                cbbReplayDate.SelectedIndex = 0;
            }

            if (cbbReplayName.Items.Count > 0)
            {
                cbbReplayName.SelectedIndex = 0;
            }
        }
    }
}
