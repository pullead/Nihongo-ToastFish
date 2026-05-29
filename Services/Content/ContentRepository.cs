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

        public VocabularyItem GetNextVocabularyItem(SQLiteConnection database, string jlptLevel, string afterContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            VocabularyItem item = QueryFirstOrDefault<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                    AND (@afterContentId IS NULL OR contentId > @afterContentId)
                  ORDER BY contentId
                  LIMIT 1",
                new { jlptLevel = level, afterContentId = NormalizeContentId(afterContentId) });

            return item ?? QueryFirstOrDefault<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId
                  LIMIT 1",
                new { jlptLevel = level });
        }

        public VocabularyItem GetPreviousVocabularyItem(SQLiteConnection database, string jlptLevel, string beforeContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            VocabularyItem item = QueryFirstOrDefault<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                    AND (@beforeContentId IS NULL OR contentId < @beforeContentId)
                  ORDER BY contentId DESC
                  LIMIT 1",
                new { jlptLevel = level, beforeContentId = NormalizeContentId(beforeContentId) });

            return item ?? QueryFirstOrDefault<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId DESC
                  LIMIT 1",
                new { jlptLevel = level });
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

        public GrammarPoint GetNextGrammarPoint(SQLiteConnection database, string jlptLevel, string afterContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarPoint item = QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT contentId, packId, jlptLevel, pattern, meaningCn, formation, usageNote, furiganaJson
                  FROM GrammarPoint
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                    AND (@afterContentId IS NULL OR contentId > @afterContentId)
                  ORDER BY contentId
                  LIMIT 1",
                new { jlptLevel = level, afterContentId = NormalizeContentId(afterContentId) });

            return item ?? QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT contentId, packId, jlptLevel, pattern, meaningCn, formation, usageNote, furiganaJson
                  FROM GrammarPoint
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId
                  LIMIT 1",
                new { jlptLevel = level });
        }

        public GrammarPoint GetPreviousGrammarPoint(SQLiteConnection database, string jlptLevel, string beforeContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarPoint item = QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT contentId, packId, jlptLevel, pattern, meaningCn, formation, usageNote, furiganaJson
                  FROM GrammarPoint
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                    AND (@beforeContentId IS NULL OR contentId < @beforeContentId)
                  ORDER BY contentId DESC
                  LIMIT 1",
                new { jlptLevel = level, beforeContentId = NormalizeContentId(beforeContentId) });

            return item ?? QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT contentId, packId, jlptLevel, pattern, meaningCn, formation, usageNote, furiganaJson
                  FROM GrammarPoint
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId DESC
                  LIMIT 1",
                new { jlptLevel = level });
        }

        public IReadOnlyList<GrammarExample> GetGrammarExamples(SQLiteConnection database, string jlptLevel, int limit = DefaultLimit)
        {
            EnsureDatabase(database);
            return Query<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern, e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                  ORDER BY e.contentId
                  LIMIT @limit",
                new { jlptLevel = NormalizeLevel(jlptLevel), limit = NormalizeLimit(limit) });
        }

        public GrammarExample GetNextGrammarExample(SQLiteConnection database, string jlptLevel, string afterContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarExample item = QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern, e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                    AND (@afterContentId IS NULL OR e.contentId > @afterContentId)
                  ORDER BY e.contentId
                  LIMIT 1",
                new { jlptLevel = level, afterContentId = NormalizeContentId(afterContentId) });

            return item ?? QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern, e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                  ORDER BY e.contentId
                  LIMIT 1",
                new { jlptLevel = level });
        }

        public GrammarExample GetPreviousGrammarExample(SQLiteConnection database, string jlptLevel, string beforeContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarExample item = QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern, e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                    AND (@beforeContentId IS NULL OR e.contentId < @beforeContentId)
                  ORDER BY e.contentId DESC
                  LIMIT 1",
                new { jlptLevel = level, beforeContentId = NormalizeContentId(beforeContentId) });

            return item ?? QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern, e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                  ORDER BY e.contentId DESC
                  LIMIT 1",
                new { jlptLevel = level });
        }

        private IReadOnlyList<T> Query<T>(SQLiteConnection database, string sql, object parameters)
        {
            return new List<T>(database.Query<T>(sql, parameters));
        }

        private T QueryFirstOrDefault<T>(SQLiteConnection database, string sql, object parameters)
        {
            return database.QueryFirstOrDefault<T>(sql, parameters);
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

        private string NormalizeContentId(string contentId)
        {
            return string.IsNullOrWhiteSpace(contentId) ? null : contentId.Trim();
        }

        private int NormalizeLimit(int limit)
        {
            if (limit <= 0)
                return DefaultLimit;
            return Math.Min(limit, MaxLimit);
        }
    }
}
