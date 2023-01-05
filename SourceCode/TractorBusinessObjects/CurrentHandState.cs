﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    //当前这把牌的状态
    [DataContract]
    [Serializable]
    public class CurrentHandState
    {
        //庄
        [DataMember]
        public string Id;
        [DataMember]
        public Dictionary<string, CurrentPoker> PlayerHoldingCards;
        [DataMember] private string _last8Holder;
        [DataMember] private int _rank;
        [DataMember] private string _starter;
        [DataMember] private Suit _trump;
        [DataMember]
        public List<TrumpState> LastTrumpStates;
        [DataMember]
        public List<int> ScoreCards;

        public CurrentHandState()
        {
            LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(4);
            Id = Guid.NewGuid().ToString();
        }

        public CurrentHandState(GameState gameState)
        {
            LastTrumpStates = new List<TrumpState>();
            ScoreCards = new List<int>();
            PlayerHoldingCards = new Dictionary<string, CurrentPoker>();
            foreach (PlayerEntity player in gameState.Players)
            {
                if (player == null) continue;
                PlayerHoldingCards[player.PlayerId] = new CurrentPoker();
            }
            LeftCardsCount = TractorRules.GetCardNumberofEachPlayer(gameState.Players.Count);
            Id = Guid.NewGuid().ToString();
        }

        [DataMember]
        public string Starter
        {
            get { return _starter; }
            set
            {
                _starter = value;
                _last8Holder = _starter;
            }
        }

        //埋底牌的玩家

        [DataMember]
        public string Last8Holder
        {
            get { return _last8Holder; }
            set { _last8Holder = value; }
        }

        //打几

        [DataMember]
        public int Rank
        {
            get { return _rank; }
            set
            {
                _rank = value;
                if (PlayerHoldingCards != null)
                {
                    foreach (var keyvalue in PlayerHoldingCards)
                    {
                        keyvalue.Value.Rank = _rank;
                    }
                }
            }
        }

        //主

        [DataMember]
        public Suit Trump
        {
            get { return _trump; }
            set
            {
                _trump = value;
                if (PlayerHoldingCards != null)
                {
                    foreach (var keyvalue in PlayerHoldingCards)
                    {
                        keyvalue.Value.Trump = _trump;
                    }
                }
            }
        }

        //亮主的牌
        [DataMember]
        public TrumpExposingPoker TrumpExposingPoker { get; set; }

        //亮主的人
        [DataMember]
        public string TrumpMaker { get; set; }

        //是否为无人亮主
        [DataMember]
        public bool IsNoTrumpMaker { get; set; }

        [DataMember]
        public HandStep CurrentHandStep { get; set; }

        [DataMember]
        public bool IsFirstHand { get; set; }

        //埋得的牌
        [DataMember]
        public int[] DiscardedCards { get; set; }

        //得分
        [DataMember]
        public int Score { get; set; }

        //得分调整：扣底基数分
        [DataMember]
        public int ScoreLast8CardsBase { get; set; }

        //得分调整：扣底倍数
        [DataMember]
        public int ScoreLast8CardsMultiplier { get; set; }

        //得分调整：甩牌罚分
        [DataMember]
        public int ScorePunishment { get; set; }

        [DataMember]
        public int LeftCardsCount { get; set; }
    }
}