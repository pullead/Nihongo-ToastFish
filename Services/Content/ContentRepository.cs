using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dapper;
using ToastFish.Model.Content;

namespace ToastFish.Services.Content
{
    public class ContentRepository
    {
        private const int DefaultLimit = 50;
        private const int MaxLimit = 500;

        public IReadOnlyList<GojuonItem> GetGojuonItems(SQLiteConnection database, int limit = DefaultLimit)
        {
            EnsureDatabase(database);
            return Query<GojuonItem>(
                database,
                @"SELECT contentId, packId, romaji, hiragana, katakana, audioPath
                  FROM GojuonItem
                  ORDER BY contentId
                  LIMIT @limit",
                new { limit = NormalizeLimit(limit) });
        }

        public IReadOnlyList<VocabularyItem> GetVocabularyItems(SQLiteConnection database, string jlptLevel, int limit = DefaultLimit)
        {
            EnsureDatabase(database);
            return Query<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId
                  LIMIT @limit",
                new { jlptLevel = NormalizeLevel(jlptLevel), limit = NormalizeLimit(limit) });
        }

        public IReadOnlyList<GrammarPoint> GetGrammarPoints(SQLiteConnection database, string jlptLevel, int limit = DefaultLimit)
        {
            EnsureDatabase(database);
            return Query<GrammarPoint>(
                database,
                @"SELECT contentId, packId, jlptLevel, pattern, meaningCn, formation, usageNote, furiganaJson
                  FROM GrammarPoint
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId
                  LIMIT @limit",
                new { jlptLevel = NormalizeLevel(jlptLevel), limit = NormalizeLimit(limit) });
        }

        public IReadOnlyList<GrammarExample> GetGrammarExamples(SQLiteConnection database, string jlptLevel, int limit = DefaultLimit)
        {
            EnsureDatabase(database);
            return Query<GrammarExample>(
                database,
                @"SELECT contentId, packId, grammarId, jlptLevel, sentenceJp, sentenceKana,
                         sentenceFuriganaJson, meaningCn, questionType, promptCn, correctAnswer, distractorsJson
                  FROM GrammarExample
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId
                  LIMIT @limit",
                new { jlptLevel = NormalizeLevel(jlptLevel), limit = NormalizeLimit(limit) });
        }

        private IReadOnlyList<T> Query<T>(SQLiteConnection database, string sql, object parameters)
        {
            return new List<T>(database.Query<T>(sql, parameters));
        }

        private void EnsureDatabase(SQLiteConnection database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
        }

        private string NormalizeLevel(string jlptLevel)
        {
            return string.IsNullOrWhiteSpace(jlptLevel) ? null : jlptLevel.Trim().ToUpperInvariant();
        }

        private int NormalizeLimit(int limit)
        {
            if (limit <= 0)
                return DefaultLimit;
            return Math.Min(limit, MaxLimit);
        }
    }
}
