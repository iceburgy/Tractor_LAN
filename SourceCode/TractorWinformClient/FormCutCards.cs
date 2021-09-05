using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Duan.Xiugang.Tractor
{
    public partial class FormCutCards : Form
    {
        public string cutType = "取消";
        public int cutPoint = 0;
        public bool noMoreCut = false;
        public FormCutCards()
        {
            InitializeComponent();
            this.cutCards_cbbCutPoint_manual.SelectedIndex = 0;
        }

        private void btnCutCards_Click(object sender, EventArgs e)
        {
            string[] nameParts = ((Button)sender).Name.Split('_');
            string cutPointString = nameParts[2];
            noMoreCut = this.cbxNoMoreCut.Checked;
            switch (cutPointString)
            {
                case "1":
                    cutType = "扒皮1张";
                    cutPoint = 1;
                    break;
                case "3":
                    cutType = "扒皮3张";
                    cutPoint = 3;
                    break;
                case "random":
                    cutType = "随机";
                    Random rand = new Random();
                    cutPoint = rand.Next(1, 108);
                    break;
                case "manual":
                    cutType = "手动";
                    cutPoint = Int32.Parse((string)cutCards_cbbCutPoint_manual.SelectedItem);
                    break;
                default:
                    break;
            }
            this.Close();
        }

        private void cbxNoMoreCut_CheckedChanged(object sender, EventArgs e)
        {
            noMoreCut = this.cbxNoMoreCut.Checked;
        }
    }
}
