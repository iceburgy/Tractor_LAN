﻿using Duan.Xiugang.Tractor.Objects;
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
            this.lblRoomName.Text = this.mainForm.ThisPlayer.CurrentRoomSetting.RoomName;
            this.lblRoomOwner.Text = this.mainForm.ThisPlayer.CurrentRoomSetting.RoomOwner;
            this.cbxAllowSurrender.Checked = this.mainForm.ThisPlayer.CurrentRoomSetting.AllowSurrender;
            this.cbxJToBottom.Checked = this.mainForm.ThisPlayer.CurrentRoomSetting.AllowJToBottom;
            this.cbbRiotByScore.SelectedIndex = this.cbbRiotByScore.FindString(this.mainForm.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewScoreCards.ToString());
            this.cbbRiotByTrump.SelectedIndex = this.cbbRiotByTrump.FindString(this.mainForm.ThisPlayer.CurrentRoomSetting.AllowRiotWithTooFewTrumpCards.ToString());

            List<int> mandRanks = this.mainForm.ThisPlayer.CurrentRoomSetting.GetManditoryRanks();
            for (int i = 0; i < 13; i++)
            {
                Control[] controls = this.Controls.Find(string.Format("cbxMust_{0}", i), true);
                if (controls != null && controls.Length > 0)
                {
                    CheckBox cbxMust = ((CheckBox)controls[0]);
                    cbxMust.Checked = mandRanks.Contains(i) ? true : false;
                }
            }

            foreach (PlayerEntity player in this.mainForm.ThisPlayer.CurrentGameState.Players)
            {
                if (player == null) continue;
                if (player.Observers.Count > 0) this.cbbKickObserver.Items.AddRange(player.Observers.ToArray());
                if (player.PlayerId == this.mainForm.ThisPlayer.MyOwnId) continue;
                this.cbbKickPlayer.Items.Add(player.PlayerId);
            }

            if (!this.mainForm.ThisPlayer.isObserver && this.mainForm.ThisPlayer.MyOwnId == this.mainForm.ThisPlayer.CurrentRoomSetting.RoomOwner)
            {
                foreach (Control control in this.Controls)
                {
                    control.Enabled = true;
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            RoomSetting rs = new RoomSetting();
            rs.RoomName = this.mainForm.ThisPlayer.CurrentRoomSetting.RoomName;
            rs.RoomOwner = this.mainForm.ThisPlayer.CurrentRoomSetting.RoomOwner;
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

        private void btnKickPlayer_Click(object sender, EventArgs e)
        {
            if (this.mainForm.ThisPlayer.CurrentHandState.CurrentHandStep == HandStep.BeforeDistributingCards ||
                this.mainForm.ThisPlayer.CurrentHandState.CurrentHandStep >= HandStep.SpecialEnding)
            {
                string toKickId = this.cbbKickPlayer.SelectedItem.ToString();
                DialogResult dialogResult = MessageBox.Show(string.Format("确定将玩家【{0}】请出房间？", toKickId), "请出房间", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    this.mainForm.ThisPlayer.ExitRoom(toKickId);
                }
            }
        }

        private void btnKickObserver_Click(object sender, EventArgs e)
        {
            string toKickId = this.cbbKickObserver.SelectedItem.ToString();
            DialogResult dialogResult = MessageBox.Show(string.Format("确定将旁观【{0}】请出房间？", toKickId), "请出房间", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                this.mainForm.ThisPlayer.ExitRoom(toKickId);
            }
        }
    }
}
