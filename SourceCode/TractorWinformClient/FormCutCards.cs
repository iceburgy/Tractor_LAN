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
        public int cutPoint = 0;
        public FormCutCards()
        {
            InitializeComponent();
            this.cutCards_cbbCutPoint_manual.SelectedIndex = 0;
        }

        private void btnCutCards_Click(object sender, EventArgs e)
        {
            string[] nameParts = ((Button)sender).Name.Split('_');
            string cutPointString = nameParts[2];
            switch (cutPointString)
            {
                case "1":
                    cutPoint = 1;
                    break;
                case "3":
                    cutPoint = 3;
                    break;
                case "random":
                    Random rand = new Random();
                    cutPoint = rand.Next(1, 108);
                    break;
                case "manual":
                    cutPoint = Int32.Parse((string)cutCards_cbbCutPoint_manual.SelectedItem);
                    break;
                default:
                    break;
            }
            this.Close();
        }
    }
}
