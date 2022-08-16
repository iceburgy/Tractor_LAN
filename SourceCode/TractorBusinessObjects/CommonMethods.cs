using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace Duan.Xiugang.Tractor.Objects
{
    /// <summary>
    ///     通用处理类.
    ///     用来处理程序中常用的方法，比如解析等.
    /// </summary>
    public static class CommonMethods
    {
        public static string replaySeparator = "===";
        public static string recoverLoginPassFlag = "RecoverLoginPass";
        public static string loginSuccessFlag = "LoginSuccess";
        public static string emailSubjectRevcoverLoginPass = "用户登录密码找回";
        public static string emailSubjectLinkPlayerEmail = "用户邮箱绑定";
        public static string emailSubjectRegisterNewPlayer = "用户注册";
        public static string reenterRoomSignal = "断线重连中,请稍后...";
        public static string resumeGameSignal = "牌局加载中,请稍后...";
        public static string[] cardNumToValue = new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        public static int winEmojiLength = 4;
        public static int nickNameOverridePassLengthLower = 5;
        public static int nickNameOverridePassLengthUpper = 10;
        public static int nickNameOverridePassMaxGetAttempts = 10;
        public static string[] dudeTints = new string[] { "", "0x00ff00", "0xffa500", "0xffff00" }; // green, orange, yellow
        public static int regcodesLength = 10;

        public static Random random = new Random();
        public static string RandomString(int lower, int upper)
        {
            int length = lower + random.Next(upper - lower + 1);
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        /// <summary>
        /// return a randome number between 0 and rangeExclusive
        /// </summary>
        /// <param name="rngCsp"></param>
        /// <param name="rangeExclusive"></param>
        /// <returns></returns>
        public static byte RandomNext(int rangeExclusive)
        {
            if (rangeExclusive <= 0 || rangeExclusive > Byte.MaxValue + 1)
                throw new ArgumentOutOfRangeException("rangeExclusive out side of a byte value");

            byte[] randomNumber = new byte[1];
            do
            {
                rngCsp.GetBytes(randomNumber);
            }
            while (!IsFairRoll(randomNumber[0], (byte)rangeExclusive));
            return (byte)((randomNumber[0] % rangeExclusive));
        }

        private static bool IsFairRoll(byte randomNumber, byte rangeExclusive)
        {
            int fullSetsOfValues = Byte.MaxValue / rangeExclusive;
            return randomNumber <= rangeExclusive * fullSetsOfValues;
        }

        public static void WriteObjectToFile(object data, string fullFolderPath, string fileName)
        {
            try
            {
                if (!Directory.Exists(fullFolderPath))
                {
                    Directory.CreateDirectory(fullFolderPath);
                }
                if (data.GetType() == typeof(GameState))
                {
                    GameState gs = DeepClone<GameState>((GameState)data);
                    foreach (PlayerEntity player in gs.Players)
                    {
                        player.Observers.Clear();
                    }
                    data = gs;
                }
                string fullFilePath = string.Format("{0}\\{1}", fullFolderPath, fileName);
                string jsonData = JsonConvert.SerializeObject(data);
                File.WriteAllText(fullFilePath, jsonData);
            }
            catch (Exception)
            {
            }
        }

        public static T ReadObjectFromFile<T>(string fullFilePath)
        {
            try
            {
                if (File.Exists(fullFilePath))
                {
                    string jsonString = File.ReadAllText(fullFilePath);
                    return JsonConvert.DeserializeObject<T>(jsonString);
                }
            }
            catch (Exception)
            {
            }
            return default(T);
        }

        public static T ReadObjectFromString<T>(string jsonString)
        {
            try
            {
                if (!string.IsNullOrEmpty(jsonString))
                {
                    return JsonConvert.DeserializeObject<T>(jsonString);
                }
            }
            catch (Exception e)
            {
            }
            return default(T);
        }

        public static string[] GetReplayEntityFullFilePath(ReplayEntity replayEntity, string folder)
        {
            string[] paths = replayEntity.ReplayId.Split(new string[] { CommonMethods.replaySeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (paths.Length != 2)
            {
                return null;
            }

            string fullFolderPath = string.Format("{0}\\{1}", folder, paths[0]);
            return new string[] { fullFolderPath, string.Format("{0}.json", paths[1]) };
        }

        public static void RotateArray<T>(T[] array, int pivot)
        {
            T[] newArray = new T[array.Length];
            Array.Copy(array, pivot, newArray, 0, array.Length - pivot);
            Array.Copy(array, 0, newArray, array.Length - pivot, pivot);
            Array.Copy(newArray, 0, array, 0, array.Length);
        }

        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        ///     得到一个牌的花色
        /// </summary>
        /// <param name="a">牌值</param>
        /// <returns>花色</returns>
        public static string GetSuitString(int a)
        {
            int suitInt = GetSuit(a);
            Suit suit = (Suit)suitInt;
            return suit.ToString();
        }

        /// <summary>
        ///     得到一个牌的点数
        /// </summary>
        /// <param name="a">牌值</param>
        /// <returns>点数</returns>
        public static string GetNumberString(int a)
        {
            if (a == 52)
            {
                return "Small";
            }
            if (a == 53)
            {
                return "Big";
            }
            return cardNumToValue[a % 13];
        }

        /// <summary>
        ///     得到一个牌的花色
        /// </summary>
        /// <param name="a">牌值</param>
        /// <returns>花色</returns>
        internal static int GetSuit(int a)
        {
            if (a >= 0 && a < 13)
            {
                return 1;
            }
            if (a >= 13 && a < 26)
            {
                return 2;
            }
            if (a >= 26 && a < 39)
            {
                return 3;
            }

            if (a >= 39 && a < 52)
            {
                return 4;
            }

            return 5;
        }

        /// <summary>
        ///     得到一张牌的花色，如果是主，则返回主的花色
        /// </summary>
        /// <param name="a">牌值</param>
        /// <param name="suit">主花色</param>
        /// <param name="rank">主Rank</param>
        /// <returns>花色</returns>
        internal static int GetSuit(int a, int suit, int rank)
        {
            int firstSuit = 0;

            if (a == 53 || a == 52)
            {
                firstSuit = suit;
            }
            else if ((a % 13) == rank)
            {
                firstSuit = suit;
            }
            else
            {
                firstSuit = GetSuit(a);
            }

            return firstSuit;
        }

        /// <summary>
        ///     从一堆牌中找出最大的牌，考虑主
        /// </summary>
        /// <param name="cards">一堆牌</param>
        /// <param name="trump">花色</param>
        /// <param name="rank"></param>
        /// <returns>最大的牌</returns>
        public static int GetMaxCard(List<int> cards, Suit trump, int rank)
        {
            var cp = new CurrentPoker();
            cp.Trump = trump;
            cp.Rank = rank;
            foreach (int card in cards)
            {
                cp.AddCard(card);
            }
            //cp.Sort();

            if (cp.IsMixed())
            {
                return -1;
            }

            if (cp.RedJoker > 0)
                return 53;
            if (cp.BlackJoker > 0)
                return 52;
            if (cp.MasterRank > 0)
                return rank + ((int)trump - 1) * 13;

            if (cp.HeartsRankTotal > 0)
                return rank;
            if (cp.SpadesRankCount > 0)
                return rank + 13;
            if (cp.DiamondsRankTotal > 0)
                return rank + 26;
            if (cp.ClubsRankTotal > 0)
                return rank + 39;

            for (int i = 51; i > -1; i--)
            {
                if (cards.Contains(i))
                    return i;
            }

            return -1;
        }

        /// <summary>
        ///     从一堆牌中找出最大的牌，考虑主
        /// </summary>
        /// <param name="cards">一堆牌</param>
        /// <param name="trump">花色</param>
        /// <param name="rank"></param>
        /// <returns>最大的牌</returns>
        public static int GetMaxCard(ArrayList cards, Suit trump, int rank)
        {
            List<int> cardsList = cards.Cast<int>().ToList();
            return GetMaxCard(cardsList, trump, rank);
        }




        /// <summary>
        ///     比较两张牌孰大孰小
        /// </summary>
        /// <param name="a">第一张牌</param>
        /// <param name="b">第二张牌</param>
        /// <param name="suit">主花色</param>
        /// <param name="rank">主Rank</param>
        /// <param name="firstSuit">第一张牌的花色</param>
        /// <returns>如果第一张大于等于第二张牌，返回true,否则返回false</returns>
        internal static bool CompareTo(int a, int b, int suit, int rank, int firstSuit)
        {
            if ((a == -1) && (b == -1))
            {
                return true;
            }
            if ((a == -1) && (b != -1))
            {
                return false;
            }
            if ((a != -1) && (b == -1))
            {
                return true;
            }


            int suit1 = GetSuit(a, suit, rank);
            int suit2 = GetSuit(b, suit, rank);

            if ((suit1 == firstSuit) && (suit2 != firstSuit))
            {
                if (suit1 == suit)
                {
                    return true;
                }
                if (suit2 == suit)
                {
                    return false;
                }
                return true;
            }
            if ((suit1 != firstSuit) && (suit2 == firstSuit))
            {
                if (suit1 == suit)
                {
                    return true;
                }
                if (suit2 == suit)
                {
                    return false;
                }

                return false;
            }

            if (a == 53)
            {
                return true;
            }


            if (a == 52)
            {
                if (b == 53)
                {
                    return false;
                }
                return true;
            }
            if (b == 52)
            {
                if (a == 53)
                {
                    return true;
                }
                return false;
            }


            if (a == (suit - 1) * 13 + rank)
            {
                if (b == 53 || b == 52)
                {
                    return false;
                }
                return true;
            }
            if (a % 13 == rank)
            {
                if (b == 53 || b == 52 || (b == (suit - 1) * 13 + rank))
                {
                    return false;
                }
                return true;
            }
            if (b == (suit - 1) * 13 + rank)
            {
                if (a == 53 || a == 52)
                {
                    return true;
                }
                return false;
            }
            if (b % 13 == rank)
            {
                if (a == 53 || a == 52 || (a == (suit - 1) * 13 + rank))
                {
                    return true;
                }
                return false;
            }
            if ((suit1 == suit) && (suit2 != suit))
            {
                return true;
            }
            if ((suit1 != suit) && (suit2 == suit))
            {
                return false;
            }
            if (suit1 == suit2)
            {
                return (a - b >= 0);
            }
            return true;
        }

        public static bool AllReady(List<PlayerEntity> Players)
        {
            foreach (PlayerEntity player in Players)
            {
                if (player == null || !player.IsReadyToStart) return false;
            }
            return true;
        }

        public static bool SomeoneBecomesReady(List<PlayerEntity> oldOnes, List<PlayerEntity> newOnes)
        {
            for (int i = 0; i < 4; i++)
            {
                if ((oldOnes[i] == null || !oldOnes[i].IsReadyToStart) && (newOnes[i] != null && newOnes[i].IsReadyToStart)) return true;
            }
            return false;
        }

        public static int GetPlayerIndexByID(List<PlayerEntity> Players, string playerID)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p != null && p.PlayerId == playerID)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}