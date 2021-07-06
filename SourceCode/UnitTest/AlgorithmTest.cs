using Duan.Xiugang.Tractor.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestProject1
{
    [TestClass]
    public class AlgorithmTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TryExposingTrumpTest()
        {
            List<Suit> availableTrump = new List<Suit>() { Suit.Heart, Suit.Spade, Suit.Diamond, Suit.Club, Suit.Joker };
            bool fullDebug = true;

            CurrentPoker currentPoker = new CurrentPoker(new[] { 53, 53 }, Suit.None, 12);
            Suit actual = Algorithm.TryExposingTrump(availableTrump, currentPoker, fullDebug);
            Assert.AreEqual(Suit.None, actual);

            currentPoker = new CurrentPoker(new[] { 53, 53, 52, 52 }, Suit.None, 12);
            actual = Algorithm.TryExposingTrump(availableTrump, currentPoker, fullDebug);
            Assert.AreEqual(Suit.Joker, actual);

            List<int> all6 = new List<int>() { 0, 0, 1, 1, 2, 2, 13, 13, 14, 14, 15, 15, 26, 26, 27, 27, 28, 28, 39, 39, 40, 40, 41, 41 };

            // all6
            currentPoker = new CurrentPoker(all6, Suit.None, 12);
            actual = Algorithm.TryExposingTrump(availableTrump, currentPoker, fullDebug);
            Assert.AreEqual(Suit.Heart, actual);

            // heart7
            all6.Add(3);
            currentPoker = new CurrentPoker(all6, Suit.None, 12);
            actual = Algorithm.TryExposingTrump(availableTrump, currentPoker, fullDebug);
            Assert.AreEqual(Suit.Heart, actual);

            // spade7
            all6.Remove(3);
            all6.Add(16);
            currentPoker = new CurrentPoker(all6, Suit.None, 12);
            actual = Algorithm.TryExposingTrump(availableTrump, currentPoker, fullDebug);
            Assert.AreEqual(Suit.Spade, actual);

            // diamond7
            all6.Remove(16);
            all6.Add(29);
            currentPoker = new CurrentPoker(all6, Suit.None, 12);
            actual = Algorithm.TryExposingTrump(availableTrump, currentPoker, fullDebug);
            Assert.AreEqual(Suit.Diamond, actual);

            // club7
            all6.Remove(29);
            all6.Add(42);
            currentPoker = new CurrentPoker(all6, Suit.None, 12);
            actual = Algorithm.TryExposingTrump(availableTrump, currentPoker, fullDebug);
            Assert.AreEqual(Suit.Club, actual);
        }
    }
}