using Duan.Xiugang.Tractor.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace TestProject1
{
    
    
    /// <summary>
    ///This is a test class for GameStateTest and is intended
    ///to contain all GameStateTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GameStateTest
    {
        /// <summary>
        ///A test for NextRank
        ///</summary>
        [TestMethod()]
        public void NextRankTest()
        {
            RoomState target = new RoomState(0);
            target.roomSetting.SetManditoryRanks(new List<int>() { 11, 8, 3 });
            target.CurrentGameState = new GameState();
            target.CurrentGameState.Players[0] = new PlayerEntity { PlayerId = "p1", Rank = 0, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = "p2", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentGameState.Players[2] = new PlayerEntity { PlayerId = "p3", Rank = 0, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = "p4", Rank = 0, Team = GameTeam.HorizonTeam };

            target.CurrentHandState = new CurrentHandState(target.CurrentGameState);

            target.CurrentHandState.Starter = "p1";
            target.CurrentHandState.Score = 75;
            var nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(1, nextStart.Rank);

            target.CurrentHandState.Starter = "p3";
            target.CurrentHandState.Score = 35;
            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(3, nextStart.Rank);

            target.CurrentHandState.Starter = "p1";
            target.CurrentHandState.Score = 0;
            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(6, nextStart.Rank);

            target.CurrentHandState.Starter = "p1";
            target.CurrentHandState.Score = 200;
            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(3, nextStart.Rank);

            target.CurrentHandState.Starter = "p2";
            target.CurrentHandState.Score = -80;
            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(8, nextStart.Rank);

            target.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = "p2", Rank = 3, Team = GameTeam.HorizonTeam };
            target.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = "p4", Rank = 3, Team = GameTeam.HorizonTeam };
            target.CurrentHandState.Starter = "p1";
            target.CurrentHandState.Score = 240;
            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(3, nextStart.Rank);

            target.roomSetting.SetManditoryRanks(new List<int>() { 3 });
            target.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = "p2", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = "p4", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentHandState.Starter = "p1";
            target.CurrentHandState.Score = 320;
            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(3, nextStart.Rank);

            target.roomSetting.SetManditoryRanks(new List<int>() { 3 });
            target.roomSetting.AllowJToBottom = true;
            target.CurrentGameState.Players[0] = new PlayerEntity { PlayerId = "p1", Rank = 3, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = "p2", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentGameState.Players[2] = new PlayerEntity { PlayerId = "p3", Rank = 3, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = "p4", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentHandState.Starter = "p2";
            target.CurrentHandState.Score = 120;
            target.CurrentHandState.Trump = Suit.Club;
            target.CurrentHandState.Rank = 0;
            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(3, nextStart.Rank);

            //副J一半
            target.roomSetting.SetManditoryRanks(new List<int>());
            target.roomSetting.AllowJToBottom = true;
            target.CurrentGameState.Players[0] = new PlayerEntity { PlayerId = "p1", Rank = 9, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = "p2", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentGameState.Players[2] = new PlayerEntity { PlayerId = "p3", Rank = 9, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = "p4", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentHandState.Starter = "p1";
            target.CurrentHandState.Score = 70;
            target.CurrentHandState.Trump = Suit.Club;
            target.CurrentHandState.Rank = 9;

            target.CurrentTrickState.ShowedCards = new Dictionary<string, List<int>>();
            target.CurrentTrickState.ShowedCards.Add("p2", new List<int>() { 9 });
            target.CurrentTrickState.Winner = "p2";

            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(9 / 2 + 1, nextStart.Rank);

            //主J到底
            target.roomSetting.SetManditoryRanks(new List<int>());
            target.roomSetting.AllowJToBottom = true;
            target.CurrentGameState.Players[0] = new PlayerEntity { PlayerId = "p1", Rank = 9, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[1] = new PlayerEntity { PlayerId = "p2", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentGameState.Players[2] = new PlayerEntity { PlayerId = "p3", Rank = 9, Team = GameTeam.VerticalTeam };
            target.CurrentGameState.Players[3] = new PlayerEntity { PlayerId = "p4", Rank = 0, Team = GameTeam.HorizonTeam };
            target.CurrentHandState.Starter = "p1";
            target.CurrentHandState.Score = 70;
            target.CurrentHandState.Trump = Suit.Heart;
            target.CurrentHandState.Rank = 9;

            target.CurrentTrickState.ShowedCards = new Dictionary<string, List<int>>();
            target.CurrentTrickState.ShowedCards.Add("p2", new List<int>() { 9 });
            target.CurrentTrickState.Winner = "p2";

            nextStart = target.CurrentGameState.NextRank(target);
            Assert.AreEqual(0 + 1, nextStart.Rank);

        }
    }
}
