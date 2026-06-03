using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using ToastFish.Model.SM2plus;
using ToastFish.Model.Storage;

namespace ToastFish.Model.SqliteControl
{
    /// <summary>
    /// 数据访问层 - 使用 Dapper ORM，所有SQL操作参数化，防止SQL注入
    /// 所有资源使用 using 语句管理，防止资源泄漏
    /// </summary>
    public class Select : IDisposable
    {
        // 静态配置
        public static string TABLE_NAME = "CET4_1";
        public static int WORD_NUMBER = 10;
        public static int ENG_TYPE = 2;
        public static int AUTO_PLAY = 1;
        public static int AUTO_LOG = 1;
        public static int NOTIFICATION_MODE = 1;

        private readonly string connectionString;
        private bool disposed = false;

        public SQLiteConnection DataBase { get; private set; }

        public IEnumerable<Word> AllWordList { get; private set; }
        public IEnumerable<JpWord> AllJpWordList { get; private set; }
        public IEnumerable<BookCount> CountList { get; private set; }

        private List<Card> NewCardLst = new List<Card>();
        private List<Card> ReviewedCardLst = new List<Card>();

        public Select()
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string dbPath = System.IO.Path.GetDirectoryName(strExeFilePath) + @"\Resources\inami.db";
            connectionString = $"Data Source={dbPath};Version=3";

            DataBase = new SQLiteConnection(connectionString);
            DataBase.Open();
            new ContentSchemaMigrator().EnsureCreated(DataBase);
        }

        /// <summary>
        /// 获取数据库连接（必须使用using）
        /// </summary>
        private SQLiteConnection GetConnection()
        {
            var conn = new SQLiteConnection(connectionString);
            conn.Open();
            return conn;
        }

        #region 更新与链接

        /// <summary>
        /// 标记单词已背过 - 参数化查询防止SQL注入
        /// </summary>
        public void UpdateWord(int WordRank)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(
                    $"UPDATE {TABLE_NAME} SET status = 1 WHERE wordRank = @WordRank",
                    new { WordRank = WordRank }
                );
            }
        }

        /// <summary>
        /// 重置单词记录
        /// </summary>
        public void ResetTableCount()
        {
            using (var conn = GetConnection())
            {
                conn.Execute($"UPDATE {TABLE_NAME} SET status = 0");
            }
        }

        /// <summary>
        /// 更新Count表 - 使用using保证资源释放
        /// </summary>
        public void UpdateTableCount()
        {
            using (var conn = GetConnection())
            {
                int count = 0;
                using (var reader = conn.ExecuteReader($"SELECT status FROM {TABLE_NAME}"))
                {
                    while (reader.Read())
                    {
                        var value = reader.GetValue(0);
                        int status = Convert.ToInt32(value);
                        if (status != 0)
                            count++;
                    }
                }

                // 参数化更新
                conn.Execute(
                    "UPDATE Count SET current = @Current WHERE bookName = @BookName",
                    new { Current = count, BookName = TABLE_NAME }
                );
            }
        }

        /// <summary>
        /// 增加计数
        /// </summary>
        public void UpdateCount()
        {
            using (var conn = GetConnection())
            {
                var countRecord = conn.QueryFirstOrDefault<BookCount>(
                    "SELECT * FROM Count WHERE bookName = @BookName",
                    new { BookName = TABLE_NAME }
                );

                if (countRecord != null)
                {
                    int newCount = countRecord.current + 1;
                    if (countRecord.bookName == "Goin")
                        newCount %= 104;

                    conn.Execute(
                        "UPDATE Count SET current = @Current WHERE bookName = @BookName",
                        new { Current = newCount, BookName = TABLE_NAME }
                    );
                }
            }
        }

        public void LoadGlobalConfig()
        {
            using (var conn = GetConnection())
            {
                var columns = new HashSet<string>();
                using (var reader = conn.ExecuteReader($"PRAGMA table_info(Global)"))
                {
                    while (reader.Read())
                    {
                        columns.Add((string)reader.GetValue(1));
                    }
                }

                if (!columns.Contains("EngType"))
                {
                    conn.Execute($"ALTER TABLE Global ADD COLUMN EngType INTEGER NOT NULL DEFAULT {ENG_TYPE}");
                }
                if (!columns.Contains("autoLog"))
                {
                    conn.Execute($"ALTER TABLE Global ADD COLUMN autoLog INTEGER NOT NULL DEFAULT {AUTO_LOG}");
                }
                if (!columns.Contains("notificationMode"))
                {
                    conn.Execute($"ALTER TABLE Global ADD COLUMN notificationMode INTEGER NOT NULL DEFAULT {NOTIFICATION_MODE}");
                }

                var globalVar = conn.QueryFirstOrDefault<Global>("SELECT * FROM Global");
                if (globalVar != null)
                {
                    WORD_NUMBER = int.TryParse(globalVar.currentWordNumber, out int wn) ? wn : 10;
                    TABLE_NAME = globalVar.currentBookName ?? "CET4_1";
                    AUTO_PLAY = globalVar.autoPlay;
                    ENG_TYPE = globalVar.EngType;
                    AUTO_LOG = globalVar.autoLog;
                    NOTIFICATION_MODE = globalVar.notificationMode;
                }
            }
        }

        public void UpdateGlobalConfig()
        {
            using (var conn = GetConnection())
            {
                conn.Execute(
                    @"UPDATE Global
                      SET currentWordNumber = @WordNumber,
                          currentBookName = @BookName,
                          autoPlay = @AutoPlay,
                          EngType = @EngType,
                          autoLog = @AutoLog,
                          notificationMode = @NotificationMode",
                    new
                    {
                        WordNumber = WORD_NUMBER,
                        BookName = TABLE_NAME,
                        AutoPlay = AUTO_PLAY,
                        EngType = ENG_TYPE,
                        AutoLog = AUTO_LOG,
                        NotificationMode = NOTIFICATION_MODE
                    }
                );
            }
        }

        public void UpdateBookName(string TableName)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(
                    "UPDATE Global SET currentBookName = @BookName",
                    new { BookName = TableName }
                );
            }
            TABLE_NAME = TableName;
        }

        public void UpdateNumber(int WordNumber)
        {
            using (var conn = GetConnection())
            {
                conn.Execute(
                    "UPDATE Global SET currentWordNumber = @Number",
                    new { Number = WordNumber }
                );
            }
            WORD_NUMBER = WordNumber;
        }

        #endregion

        #region 英语部分

        /// <summary>
        /// 查询当前单词表当前进度
        /// </summary>
        public List<int> SelectCount()
        {
            using (var conn = GetConnection())
            {
                var record = conn.QueryFirstOrDefault<BookCount>(
                    "SELECT * FROM Count WHERE bookName = @BookName",
                    new { BookName = TABLE_NAME }
                );

                if (record != null)
                {
                    return new List<int> { record.current, record.number };
                }
                return new List<int> { 0, 0 };
            }
        }

        /// <summary>
        /// 查找某本书的所有单词
        /// </summary>
        public void SelectWordList()
        {
            if (TABLE_NAME.IndexOf("自定义") != -1)
                TABLE_NAME = "GRE_2";

            using (var conn = GetConnection())
            {
                EnsureCardColumns(conn, TABLE_NAME);
                AllWordList = conn.Query<Word>($"SELECT * FROM {TABLE_NAME}");

                NewCardLst.Clear();
                ReviewedCardLst.Clear();

                foreach (var word in AllWordList)
                {
                    var card = new Card(word);
                    if (card.status != Cardstatus.Reviewed)
                        NewCardLst.Add(card);
                    else
                        ReviewedCardLst.Add(card);
                }
            }
        }

        private void EnsureCardColumns(SQLiteConnection conn, string tableName)
        {
            var columns = new HashSet<string>();
            using (var reader = conn.ExecuteReader($"PRAGMA table_info({tableName})"))
            {
                while (reader.Read())
                {
                    columns.Add((string)reader.GetValue(1));
                }
            }

            var requiredColumns = new[]
            {
                new { Name = "difficulty", DefaultValue = Parameters.diffcultyDefaultValue.ToString() },
                new { Name = "daysBetweenReviews", DefaultValue = Parameters.daysBetweenReviewsDefaultValue.ToString() },
                new { Name = "lastScore", DefaultValue = "0" },
                new { Name = "dateLastReviewed", DefaultValue = "NULL" }
            };

            foreach (var column in requiredColumns)
            {
                if (!columns.Contains(column.Name))
                {
                    string cmd = column.DefaultValue == "NULL"
                        ? $"ALTER TABLE {tableName} ADD COLUMN {column.Name} TEXT DEFAULT {column.DefaultValue}"
                        : $"ALTER TABLE {tableName} ADD COLUMN {column.Name} REAL NOT NULL DEFAULT {column.DefaultValue}";

                    conn.Execute(cmd);
                }
            }
        }

        public void updateCardDateBase(List<Card> cardList)
        {
            using (var conn = GetConnection())
            {
                foreach (var card in cardList)
                {
                    conn.Execute(
                        $@"UPDATE {TABLE_NAME}
                           SET status = @Status,
                               difficulty = @Difficulty,
                               daysBetweenReviews = @DaysBetweenReviews,
                               lastScore = @LastScore,
                               dateLastReviewed = @DateLastReviewed
                           WHERE wordRank = @WordRank",
                        new
                        {
                            Status = (int)card.status,
                            Difficulty = card.difficulty,
                            DaysBetweenReviews = card.daysBetweenReviews,
                            LastScore = card.lastScore,
                            DateLastReviewed = card.dateLastReviewed,
                            WordRank = card.word.wordRank
                        }
                    );
                }
            }
        }

        public void GetOverdueReviewedCardList(int maxReviewedCardNumer, out List<Card> usedReviewedCardLst)
        {
            usedReviewedCardLst = new List<Card>();

            if (ReviewedCardLst.Count() < maxReviewedCardNumer)
                maxReviewedCardNumer = ReviewedCardLst.Count();

            ReviewedCardLst.Sort((b, a) =>
            {
                int result = a.percentOverdue.CompareTo(b.percentOverdue);
                return result;
            });

            for (int i = 0; i < maxReviewedCardNumer; i++)
            {
                Card card0 = ReviewedCardLst[0];
                usedReviewedCardLst.Add(card0);
                ReviewedCardLst.RemoveAt(0);
            }
        }

        public void GenerateRandomNewCardList(int maxNewCardNumber, out List<Card> usedNewCardLst)
        {
            usedNewCardLst = new List<Card>();

            if (NewCardLst.Count() < maxNewCardNumber)
                maxNewCardNumber = NewCardLst.Count();

            Random Rd = new Random();
            for (int i = 0; i < maxNewCardNumber; i++)
            {
                int Index = Rd.Next(NewCardLst.Count);
                usedNewCardLst.Add(NewCardLst[Index]);
                NewCardLst.RemoveAt(Index);
            }
        }


        /// <summary>
        /// 从词库里随机选择Number个单词
        /// </summary>
        /// <typeparam name="List<Word>"></typeparam>
        /// <param name="Number"></param>
        /// <returns></returns>
        public List<Word> GetRandomWordList(int Number)
        {
            List<Word> Result = new List<Word>();
            //SelectWordList();
            //var AllWordArray = AllWordList.ToList();



            //把所有没背过的单词序号都存在WordList里了
            List<Word> WordList = new List<Word>();
            foreach (var Word in AllWordList)
            {
                if (Word.status == 0) //单词是否背过
                {
                    WordList.Add(Word);
                }
            }

            if (WordList.Count() == 0)
                return Result;
            else if (WordList.Count() < Number)
                Number = WordList.Count();

            Random Rd = new Random();
            for (int i = 0; i < Number; i++)
            {
                int Index = Rd.Next(WordList.Count);//下标
                Result.Add(WordList[Index]);
                WordList.RemoveAt(Index);
            }
            return Result;
        }

        /// <summary>
        /// 获取俩随机单词，作为错误答案
        /// </summary>
        public List<Word> GetRandomWords(int Number)
        {
            List<Word> Result = new List<Word>();
            //SelectWordList();
            var AllWordArray = AllWordList.ToList();

            Random Rd = new Random();
            for (int i = 0; i < Number; i++)
            {
                int Index = Rd.Next(AllWordArray.Count);//下标
                Result.Add(AllWordArray[Index]);
                AllWordArray.RemoveAt(Index);
            }
            return Result;
        }
        #endregion

        #region 日语部分
        /// <summary>
        /// 查找某本书的所有单词
        /// </summary>
        public void SelectJpWordList()
        {
            using (var conn = GetConnection())
            {
                JpWord Temp = new JpWord();
                AllJpWordList = conn.Query<JpWord>("select * from " + TABLE_NAME, Temp);
            }
        }

        /// <summary>
        /// 从词库里随机选择Number个单词
        /// </summary>
        /// <typeparam name="List<Word>"></typeparam>
        /// <param name="Number"></param>
        /// <returns></returns>
        public List<JpWord> GetRandomJpWordList(int Number)
        {
            List<JpWord> Result = new List<JpWord>();
            SelectJpWordList();
            var AllWordArray = AllJpWordList.ToList();

            //把所有没背过的单词序号都存在WordList里了
            List<int> WordList = new List<int>();
            foreach (var JpWord in AllJpWordList)
            {
                if (JpWord.status == 0) //单词是否背过
                {
                    WordList.Add(JpWord.wordRank);
                }
            }

            if (WordList.Count() == 0)
                return Result;
            else if (WordList.Count() < Number)
                Number = WordList.Count();

            Random Rd = new Random();
            for (int i = 0; i < Number; i++)
            {
                int Index = Rd.Next(WordList.Count);//下标
                Result.Add(AllWordArray[Index]);
                AllWordArray.RemoveAt(Index);
            }
            return Result;
        }

        /// <summary>
        /// 获取俩随机单词，作为错误答案
        /// </summary>
        public List<JpWord> GetRandomJpWords(int Number)
        {
            List<JpWord> Result = new List<JpWord>();
            SelectJpWordList();
            var AllWordArray = AllJpWordList.ToList();

            Random Rd = new Random();
            for (int i = 0; i < Number; i++)
            {
                int Index = Rd.Next(AllWordArray.Count);//下标
                Result.Add(AllWordArray[Index]);
                AllWordArray.RemoveAt(Index);
            }
            return Result;
        }
        #endregion

        #region 五十音部分
        public List<GoinWord> GetGainWordList()
        {
            using (var conn = GetConnection())
            {
                GoinWord Temp = new GoinWord();
                IEnumerable<GoinWord> AllGoinWordList = conn.Query<GoinWord>("select * from " + TABLE_NAME, Temp);
                return AllGoinWordList.ToList();
            }
        }

        public int GetGoinProgress()
        {
            using (var conn = GetConnection())
            {
                BookCount Temp = new BookCount();
                CountList = conn.Query<BookCount>("select * from Count where bookName = 'Goin'", Temp);
                var CountArray = CountList.ToList();
                return CountArray[0].current;
            }
        }

        public List<GoinWord> GetTwoGoinRandomWords(GoinWord CurrentWord)
        {
            List<GoinWord> Result = new List<GoinWord>();
            List<GoinWord> WordList = GetGainWordList();

            Random Rd = new Random();
            for (int i = 0; i < 2; i++)
            {
                int Index = Rd.Next(WordList.Count);//下标
                if (CurrentWord.wordRank == Index + 1)
                {
                    i--;
                    continue;
                }
                Result.Add(WordList[Index]);
                WordList.RemoveAt(Index);
            }
            return Result;
        }
        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    AllWordList = null;
                    AllJpWordList = null;
                    CountList = null;
                    NewCardLst?.Clear();
                    ReviewedCardLst?.Clear();
                    DataBase?.Dispose();
                }

                disposed = true;
            }
        }

        #endregion
    }

    #region 查询类
    [Serializable]
    public class Word
    {
        public int wordRank { get; set; }
        public int status { get; set; }
        public String headWord { get; set; }
        public String usPhone { get; set; }
        public String ukPhone { get; set; }
        public String usSpeech { get; set; }
        public String ukSpeech { get; set; }
        public String tranCN { get; set; }
        public String pos { get; set; }
        public String tranOther { get; set; }
        public String question { get; set; }
        public String explain { get; set; }
        public String rightIndex { get; set; }
        public String examType { get; set; }
        public String choiceIndexOne { get; set; }
        public String choiceIndexTwo { get; set; }
        public String choiceIndexThree { get; set; }
        public String choiceIndexFour { get; set; }
        public String sentence { get; set; }
        public String sentenceCN { get; set; }
        public String phrase { get; set; }
        public String phraseCN { get; set; }
        public double difficulty { get; set; }
        public double daysBetweenReviews { get; set; }
        public double lastScore { get; set; }
        public String dateLastReviewed { get; set; }
    }

    [Serializable]
    public class BookCount
    {
        public String bookName { get; set; }
        public int number { get; set; }
        public int current { get; set; }
    }

    [Serializable]
    public class GoinWord
    {
        public int wordRank { get; set; }
        public string bookId { get; set; }
        public int status { get; set; }
        public string romaji { get; set; }
        public string hiragana { get; set; }
        public string katakana { get; set; }

    }

    [Serializable]
    public class Global
    {
        public string currentWordNumber { get; set; }
        public string currentBookName { get; set; }
        public int autoPlay { get; set; }
        public int EngType { get; set; }
        public int autoLog { get; set; }
        public int notificationMode { get; set; }
    }

    [Serializable]
    public class JpWord
    {
        public int wordRank { get; set; }
        public string bookId { get; set; }
        public int status { get; set; }
        public String headWord { get; set; }
        public int Phone { get; set; }
        public String tranCN { get; set; }
        public String pos { get; set; }
        public String hiragana { get; set; }
    }

    [Serializable]
    public class CustomizeWord
    {
        public String firstLine { get; set; }
        public String secondLine { get; set; }
        public String thirdLine { get; set; }
        public String fourthLine { get; set; }
    }
    #endregion
}
