using Duan.Xiugang.Tractor.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TractorServer;

namespace TestProject1
{


    /// <summary>
    ///This is a test class for GameRoomTest and is intended
    ///to contain all GameRoomTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GameRoomTest
    {
        /// <summary>
        ///A test for CalculatePointsFromDiscarded8Cards
        ///</summary>
        [TestMethod()]
        public void CalculatePointsFromDiscarded8CardsTest()
        {
            GameRoom gameRoom = new GameRoom(0, "test", null);
            gameRoom.CurrentRoomState.CurrentGameState = new GameState();
            gameRoom.CurrentRoomState.CurrentGameState.Players[0] = new PlayerEntity { PlayerId = "p1", Rank = 0, Team = GameTeam.VerticalTeam };
            gameRoom.CurrentRoomState.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = "p2", Rank = 0, Team = GameTeam.HorizonTeam };
            gameRoom.CurrentRoomState.CurrentGameState.Players[2] = new PlayerEntity { PlayerId = "p3", Rank = 0, Team = GameTeam.VerticalTeam };
            gameRoom.CurrentRoomState.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = "p4", Rank = 0, Team = GameTeam.HorizonTeam };

            gameRoom.CurrentRoomState.CurrentHandState.Starter = "p1";
            gameRoom.CurrentRoomState.CurrentHandState.Trump = Suit.Spade;
            gameRoom.CurrentRoomState.CurrentHandState.Rank = 2;
            gameRoom.CurrentRoomState.CurrentHandState.LeftCardsCount = 0;
            //底牌10分
            gameRoom.CurrentRoomState.CurrentHandState.DiscardedCards = new int[] { 1, 2, 9, 4, 5, 6, 7, 8 };

            gameRoom.CurrentRoomState.CurrentTrickState.Learder = "p1";
            gameRoom.CurrentRoomState.CurrentTrickState.Winner = "p2";

            //带一对敲低，但领出牌中没有对子，所以底牌分最终为：20
            gameRoom.CurrentRoomState.CurrentHandState.Score = 0;
            gameRoom.CurrentRoomState.CurrentHandState.ScoreLast8CardsBase = 0;
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p1"] = new List<int>() { 12, 11, 10 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p2"] = new List<int>() { 25, 25, 24 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p3"] = new List<int>() { 1, 2, 3 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p4"] = new List<int>() { 4, 5, 6 };
            gameRoom.CalculatePointsFromDiscarded8Cards();
            Assert.AreEqual(20, gameRoom.CurrentRoomState.CurrentHandState.Score);

            //带一对敲低，且领出牌中有对子，所以底牌分最终为：40
            gameRoom.CurrentRoomState.CurrentHandState.Score = 0;
            gameRoom.CurrentRoomState.CurrentHandState.ScoreLast8CardsBase = 0;
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p1"] = new List<int>() { 12, 12, 11 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p2"] = new List<int>() { 25, 25, 24 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p3"] = new List<int>() { 1, 2, 3 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p4"] = new List<int>() { 4, 5, 6 };
            gameRoom.CalculatePointsFromDiscarded8Cards();
            Assert.AreEqual(40, gameRoom.CurrentRoomState.CurrentHandState.Score);

            //带2对拖拉机敲低，且自己为领出，底牌分最终为：160
            gameRoom.CurrentRoomState.CurrentHandState.Score = 0;
            gameRoom.CurrentRoomState.CurrentHandState.ScoreLast8CardsBase = 0;
            gameRoom.CurrentRoomState.CurrentTrickState.Learder = "p2";
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p1"] = new List<int>() { 12, 12, 11, 10 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p2"] = new List<int>() { 25, 25, 24, 24 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p3"] = new List<int>() { 1, 2, 3, 4 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p4"] = new List<int>() { 4, 5, 6, 7 };
            gameRoom.CalculatePointsFromDiscarded8Cards();
            Assert.AreEqual(160, gameRoom.CurrentRoomState.CurrentHandState.Score);

            //带3对拖拉机敲低，且自己为领出，底牌分最终为：640
            gameRoom.CurrentRoomState.CurrentHandState.Score = 0;
            gameRoom.CurrentRoomState.CurrentHandState.ScoreLast8CardsBase = 0;
            gameRoom.CurrentRoomState.CurrentTrickState.Learder = "p2";
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p1"] = new List<int>() { 12, 12, 11, 10, 9, 8 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p2"] = new List<int>() { 25, 25, 24, 24, 23, 23 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p3"] = new List<int>() { 1, 2, 3, 4, 5, 6 };
            gameRoom.CurrentRoomState.CurrentTrickState.ShowedCards["p4"] = new List<int>() { 4, 5, 6, 7, 8, 9 };
            gameRoom.CalculatePointsFromDiscarded8Cards();
            Assert.AreEqual(640, gameRoom.CurrentRoomState.CurrentHandState.Score);

        }
    }
}
