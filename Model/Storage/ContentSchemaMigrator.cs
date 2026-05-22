using System;
using System.Data.SQLite;

namespace ToastFish.Model.Storage
{
    public class ContentSchemaMigrator
    {
        public void EnsureCreated(SQLiteConnection database)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            string[] commands =
            {
                @"CREATE TABLE IF NOT EXISTS ContentSource (
                    sourceId TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    url TEXT,
                    licenseName TEXT,
                    licenseUrl TEXT,
                    attribution TEXT,
                    notes TEXT
                )",
                @"CREATE TABLE IF NOT EXISTS ContentPack (
                    packId TEXT PRIMARY KEY,
                    version TEXT NOT NULL,
                    jlptLevel TEXT NOT NULL,
                    contentKind TEXT NOT NULL,
                    displayName TEXT NOT NULL,
                    description TEXT,
                    sourceId TEXT,
                    licenseName TEXT,
                    licenseUrl TEXT,
                    contentHash TEXT,
                    installedAt TEXT NOT NULL,
                    FOREIGN KEY(sourceId) REFERENCES ContentSource(sourceId)
                )",
                @"CREATE TABLE IF NOT EXISTS VocabularyItem (
                    contentId TEXT PRIMARY KEY,
                    packId TEXT NOT NULL,
                    jlptLevel TEXT NOT NULL,
                    headword TEXT NOT NULL,
                    reading TEXT,
                    furiganaJson TEXT,
                    meaningCn TEXT NOT NULL,
                    partOfSpeech TEXT,
                    exampleJp TEXT,
                    exampleKana TEXT,
                    exampleFuriganaJson TEXT,
                    exampleCn TEXT,
                    FOREIGN KEY(packId) REFERENCES ContentPack(packId)
                )",
                @"CREATE TABLE IF NOT EXISTS GrammarPoint (
                    contentId TEXT PRIMARY KEY,
                    packId TEXT NOT NULL,
                    jlptLevel TEXT NOT NULL,
                    pattern TEXT NOT NULL,
                    meaningCn TEXT NOT NULL,
                    formation TEXT,
                    usageNote TEXT,
                    furiganaJson TEXT,
                    FOREIGN KEY(packId) REFERENCES ContentPack(packId)
                )",
                @"CREATE TABLE IF NOT EXISTS GrammarExample (
                    contentId TEXT PRIMARY KEY,
                    packId TEXT NOT NULL,
                    grammarId TEXT,
                    jlptLevel TEXT NOT NULL,
                    sentenceJp TEXT NOT NULL,
                    sentenceKana TEXT,
                    sentenceFuriganaJson TEXT,
                    meaningCn TEXT NOT NULL,
                    questionType TEXT,
                    promptCn TEXT,
                    correctAnswer TEXT,
                    distractorsJson TEXT,
                    FOREIGN KEY(packId) REFERENCES ContentPack(packId),
                    FOREIGN KEY(grammarId) REFERENCES GrammarPoint(contentId)
                )",
                @"CREATE TABLE IF NOT EXISTS GojuonItem (
                    contentId TEXT PRIMARY KEY,
                    packId TEXT NOT NULL,
                    romaji TEXT NOT NULL,
                    hiragana TEXT NOT NULL,
                    katakana TEXT NOT NULL,
                    audioPath TEXT,
                    FOREIGN KEY(packId) REFERENCES ContentPack(packId)
                )",
                @"CREATE TABLE IF NOT EXISTS ReviewCard (
                    reviewCardId TEXT PRIMARY KEY,
                    contentId TEXT NOT NULL,
                    contentKind TEXT NOT NULL,
                    status TEXT NOT NULL,
                    dueAt TEXT,
                    lastReviewedAt TEXT,
                    reviewCount INTEGER NOT NULL DEFAULT 0,
                    easeFactor REAL NOT NULL DEFAULT 2.5,
                    intervalDays REAL NOT NULL DEFAULT 0,
                    lapses INTEGER NOT NULL DEFAULT 0,
                    UNIQUE(contentId, contentKind)
                )",
                "CREATE INDEX IF NOT EXISTS IX_ContentPack_LevelKind ON ContentPack(jlptLevel, contentKind)",
                "CREATE INDEX IF NOT EXISTS IX_VocabularyItem_Pack ON VocabularyItem(packId)",
                "CREATE INDEX IF NOT EXISTS IX_GrammarPoint_Pack ON GrammarPoint(packId)",
                "CREATE INDEX IF NOT EXISTS IX_GrammarExample_Pack ON GrammarExample(packId)",
                "CREATE INDEX IF NOT EXISTS IX_GojuonItem_Pack ON GojuonItem(packId)",
                "CREATE INDEX IF NOT EXISTS IX_ReviewCard_Due ON ReviewCard(dueAt, status)"
            };

            foreach (string commandText in commands)
            {
                using (SQLiteCommand command = database.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
