using Duan.Xiugang.Tractor.Objects;
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
    public delegate void RoomSettingChangedByClientEventHandler();

    public partial class FormRoomSetting : Form
    {
        public event RoomSettingChangedByClientEventHandler RoomSettingChangedByClientEvent;
        MainForm mainForm;
        internal FormRoomSetting(MainForm mainForm)
        {
            this.mainForm = mainForm;
            InitializeComponent();

            InitValues();
        }

        private void InitValues()
        {
            this.cbxAllowSurrender.Checked = this.mainForm.ThisPlayer.CurrentRoomSetting.AllowSurrender;
            this.cbxJToBottom.Checked = this.mainForm.ThisPlayer.CurrentRoomSetting.AllowJToBottom;
            this.cbbRiotByScore.SelectedIndex = this.cbbRiotByScore.FindString(this.mainForm.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards.ToString());
            this.cbbRiotByTrump.SelectedIndex = this.cbbRiotByTrump.FindString(this.mainForm.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewTrumpCards.ToString());

            List<int> mandRanks = this.mainForm.ThisPlayer.CurrentRoomSetting.GetManditoryRanks();
            for (int i = 0; i < 12; i++)
            {
                Control[] controls = this.Controls.Find(string.Format("cbxMust_{0}", i), true);
                if (controls != null && controls.Length > 0)
                {
                    CheckBox cbxMust = ((CheckBox)controls[0]);
                    cbxMust.Checked = mandRanks.Contains(i) ? true : false;
                }
            }

            if (this.mainForm.ThisPlayer.CurrentGameState.Players[0] == null || this.mainForm.ThisPlayer.CurrentGameState.Players[0].PlayerId != this.mainForm.ThisPlayer.PlayerId)
            {
                foreach (Control control in this.Controls)
                {
                    control.Enabled = false;
                }
                this.btnCancel.Enabled = true;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            RoomSetting rs = new RoomSetting();
            rs.AllowSurrender = this.cbxAllowSurrender.Checked;
            rs.AllowJToBottom = this.cbxJToBottom.Checked;
            rs.AllowRiotWithTooFewScoreCards = Int32.Parse((string)this.cbbRiotByScore.SelectedItem);
            rs.AllowRiotWithTooFewTrumpCards = Int32.Parse((string)this.cbbRiotByTrump.SelectedItem);

            List<int> mandRanks = new List<int>();
            for (int i = 0; i < 13; i++)
            {
                Control[] controls = this.Controls.Find(string.Format("cbxMust_{0}", i), true);
                if (controls != null && controls.Length > 0)
                {
                    CheckBox cbxMust = ((CheckBox)controls[0]);
                    if (cbxMust.Checked)
                    {
                        mandRanks.Add(i);
                    }
                }
            }
            rs.SetManditoryRanks(mandRanks);

            if (!this.mainForm.ThisPlayer.CurrentRoomSetting.Equals(rs))
            {
                this.mainForm.ThisPlayer.CurrentRoomSetting = rs;
                RoomSettingChangedByClientEvent();
            }
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
