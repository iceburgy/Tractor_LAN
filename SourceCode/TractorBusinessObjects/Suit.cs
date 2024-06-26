﻿using System;
namespace Duan.Xiugang.Tractor.Objects
{
    [Serializable]
    public enum Suit
    {
        None,
        Heart,
        Spade,
        Diamond,
        Club,
        Joker
    }

    [Serializable]
    public enum TrumpExposingPoker
    {
        None,
        SingleRank,
        PairRank,
        PairBlackJoker,
        PairRedJoker
    }

    //每把牌的不同阶段
    [Serializable]
    public enum HandStep
    {
        BeforeDistributingCards,
        DistributingCards,
        DistributingCardsFinished,
        DistributingLast8Cards,
        DistributingLast8CardsFinished,
        DiscardingLast8Cards,
        DiscardingLast8CardsFinished,
        Last8CardsRobbed,
        Playing,
        SpecialEnding,
        Ending
    }

    //特殊结束几种情况
    [Serializable]
    public enum SpecialEndingType
    {
        Surrender,
        RiotByScore,
        RiotByTrump
    }
}