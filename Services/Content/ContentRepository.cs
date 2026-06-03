using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
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
            IReadOnlyList<VocabularyItem> items = Query<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId
                  LIMIT @limit",
                new { jlptLevel = NormalizeLevel(jlptLevel), limit = NormalizeLimit(limit) });
            PopulateVocabularyDisplayDetails(database, items);
            return items;
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

            item = item ?? QueryFirstOrDefault<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId
                  LIMIT 1",
                new { jlptLevel = level });

            PopulateVocabularyDisplayDetails(database, item);
            return item;
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

            item = item ?? QueryFirstOrDefault<VocabularyItem>(
                database,
                @"SELECT contentId, packId, jlptLevel, headword, reading, furiganaJson,
                         meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn
                  FROM VocabularyItem
                  WHERE (@jlptLevel IS NULL OR jlptLevel = @jlptLevel)
                  ORDER BY contentId DESC
                  LIMIT 1",
                new { jlptLevel = level });

            PopulateVocabularyDisplayDetails(database, item);
            return item;
        }

        public IReadOnlyList<GrammarPoint> GetGrammarPoints(SQLiteConnection database, string jlptLevel, int limit = DefaultLimit)
        {
            EnsureDatabase(database);
            IReadOnlyList<GrammarPoint> items = Query<GrammarPoint>(
                database,
                @"SELECT g.contentId, g.packId, g.jlptLevel, g.pattern, g.meaningCn, g.formation, g.usageNote, g.furiganaJson,
                         ex.sentenceJp AS exampleSentenceJp, ex.sentenceKana AS exampleSentenceKana,
                         ex.sentenceFuriganaJson AS exampleFuriganaJson, ex.meaningCn AS exampleMeaningCn
                  FROM GrammarPoint g
                  LEFT JOIN GrammarExample ex ON ex.contentId = (
                      SELECT e.contentId FROM GrammarExample e
                      WHERE e.grammarId = g.contentId
                      ORDER BY e.contentId
                      LIMIT 1
                  )
                  WHERE (@jlptLevel IS NULL OR g.jlptLevel = @jlptLevel)
                  ORDER BY g.contentId
                  LIMIT @limit",
                new { jlptLevel = NormalizeLevel(jlptLevel), limit = NormalizeLimit(limit) });
            PopulateGrammarPointExamples(database, items);
            return items;
        }

        public GrammarPoint GetNextGrammarPoint(SQLiteConnection database, string jlptLevel, string afterContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarPoint item = QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT g.contentId, g.packId, g.jlptLevel, g.pattern, g.meaningCn, g.formation, g.usageNote, g.furiganaJson,
                         ex.sentenceJp AS exampleSentenceJp, ex.sentenceKana AS exampleSentenceKana,
                         ex.sentenceFuriganaJson AS exampleFuriganaJson, ex.meaningCn AS exampleMeaningCn
                  FROM GrammarPoint g
                  LEFT JOIN GrammarExample ex ON ex.contentId = (
                      SELECT e.contentId FROM GrammarExample e
                      WHERE e.grammarId = g.contentId
                      ORDER BY e.contentId
                      LIMIT 1
                  )
                  WHERE (@jlptLevel IS NULL OR g.jlptLevel = @jlptLevel)
                    AND (@afterContentId IS NULL OR g.contentId > @afterContentId)
                  ORDER BY g.contentId
                  LIMIT 1",
                new { jlptLevel = level, afterContentId = NormalizeContentId(afterContentId) });

            item = item ?? QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT g.contentId, g.packId, g.jlptLevel, g.pattern, g.meaningCn, g.formation, g.usageNote, g.furiganaJson,
                         ex.sentenceJp AS exampleSentenceJp, ex.sentenceKana AS exampleSentenceKana,
                         ex.sentenceFuriganaJson AS exampleFuriganaJson, ex.meaningCn AS exampleMeaningCn
                  FROM GrammarPoint g
                  LEFT JOIN GrammarExample ex ON ex.contentId = (
                      SELECT e.contentId FROM GrammarExample e
                      WHERE e.grammarId = g.contentId
                      ORDER BY e.contentId
                      LIMIT 1
                  )
                  WHERE (@jlptLevel IS NULL OR g.jlptLevel = @jlptLevel)
                  ORDER BY g.contentId
                  LIMIT 1",
                new { jlptLevel = level });

            PopulateGrammarPointExamples(database, item);
            return item;
        }

        private void PopulateVocabularyDisplayDetails(SQLiteConnection database, VocabularyItem item)
        {
            if (item == null)
                return;

            PopulateVocabularyDisplayDetails(database, new[] { item });
        }

        private void PopulateVocabularyDisplayDetails(SQLiteConnection database, IEnumerable<VocabularyItem> items)
        {
            if (items == null)
                return;

            foreach (VocabularyItem item in items)
            {
                if (item == null)
                    continue;

                item.sourceTags = FormatVocabularySourceTags(item.partOfSpeech, item.jlptLevel);
                item.partOfSpeech = ExtractRealPartOfSpeech(item.partOfSpeech);
                PopulateRelatedGrammarExample(database, item);
            }
        }

        private void PopulateRelatedGrammarExample(SQLiteConnection database, VocabularyItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.headword) || item.headword.Trim().Length < 2)
                return;

            List<GrammarExample> matches = new List<GrammarExample>(database.Query<GrammarExample>(
                @"SELECT e.sentenceJp, e.meaningCn,
                         g.pattern AS grammarPattern, g.meaningCn AS grammarMeaningCn
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE e.sentenceJp LIKE @pattern
                  ORDER BY LENGTH(e.sentenceJp), e.contentId
                  LIMIT 6",
                new { pattern = "%" + item.headword.Trim() + "%" }));

            if (matches.Count == 0)
                return;

            GrammarExample grammarExample = matches[0];
            item.relatedGrammarPattern = grammarExample.grammarPattern;
            item.relatedGrammarMeaningCn = grammarExample.grammarMeaningCn;
            item.relatedGrammarExampleJp = grammarExample.sentenceJp;
            item.relatedGrammarExampleCn = grammarExample.meaningCn;

            GrammarExample vocabularyExample = FindDifferentVocabularyExample(matches, item.exampleJp, grammarExample.sentenceJp);
            if (vocabularyExample != null && (string.IsNullOrWhiteSpace(item.exampleJp) ||
                string.Equals(item.exampleJp.Trim(), grammarExample.sentenceJp, StringComparison.Ordinal)))
            {
                item.exampleJp = vocabularyExample.sentenceJp;
                item.exampleCn = vocabularyExample.meaningCn;
            }
            else if (string.Equals(item.exampleJp == null ? null : item.exampleJp.Trim(), grammarExample.sentenceJp, StringComparison.Ordinal))
            {
                item.exampleJp = string.Empty;
                item.exampleCn = string.Empty;
            }
        }

        private GrammarExample FindDifferentVocabularyExample(
            IEnumerable<GrammarExample> candidates,
            string currentExample,
            string grammarExample)
        {
            foreach (GrammarExample candidate in candidates)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.sentenceJp))
                    continue;

                string sentence = candidate.sentenceJp.Trim();
                if (!string.IsNullOrWhiteSpace(currentExample) &&
                    string.Equals(sentence, currentExample.Trim(), StringComparison.Ordinal))
                    continue;

                if (!string.IsNullOrWhiteSpace(grammarExample) &&
                    string.Equals(sentence, grammarExample.Trim(), StringComparison.Ordinal))
                    continue;

                return candidate;
            }

            return null;
        }

        private string ExtractRealPartOfSpeech(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return string.Empty;

            string value = rawValue.Trim();
            string[] knownParts =
            {
                "名词", "动词", "形容词", "副词", "助词", "连体词", "接续词", "感叹词",
                "代词", "数词", "助动词", "前缀", "后缀", "表达", "短语",
                "noun", "verb", "adjective", "adverb", "particle", "pronoun"
            };

            foreach (string part in knownParts)
            {
                if (value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                    return NormalizePartOfSpeech(part);
            }

            return string.Empty;
        }

        private string NormalizePartOfSpeech(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "noun":
                    return "名词";
                case "verb":
                    return "动词";
                case "adjective":
                    return "形容词";
                case "adverb":
                    return "副词";
                case "particle":
                    return "助词";
                case "pronoun":
                    return "代词";
                default:
                    return value;
            }
        }

        private string FormatVocabularySourceTags(string rawValue, string jlptLevel)
        {
            List<string> values = new List<string>();
            if (!string.IsNullOrWhiteSpace(jlptLevel))
                values.Add(jlptLevel.Trim().ToUpperInvariant());

            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                foreach (string token in rawValue.Split(' '))
                {
                    string normalized = FormatVocabularySourceTag(token);
                    if (!string.IsNullOrWhiteSpace(normalized) && !values.Contains(normalized))
                        values.Add(normalized);
                }
            }

            return string.Join(" / ", values);
        }

        private string FormatVocabularySourceTag(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            string value = token.Trim();
            if (value == "JLPT" || value == "Genki" || value == "Intermediate_Japanese")
                return string.Empty;
            if (value.StartsWith("JLPT_N", StringComparison.OrdinalIgnoreCase))
                return value.Replace("JLPT_", string.Empty).ToUpperInvariant();
            if (value.StartsWith("JLPT_", StringComparison.OrdinalIgnoreCase))
                return value.Replace("JLPT_", "N").ToUpperInvariant();
            if (value.StartsWith("Genki_Ln.", StringComparison.OrdinalIgnoreCase))
                return "Genki 第" + value.Substring("Genki_Ln.".Length) + "课";
            if (value.StartsWith("Intermediate_Japanese_Ln.", StringComparison.OrdinalIgnoreCase))
                return "Intermediate 第" + value.Substring("Intermediate_Japanese_Ln.".Length) + "课";

            return string.Empty;
        }

        public GrammarPoint GetPreviousGrammarPoint(SQLiteConnection database, string jlptLevel, string beforeContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarPoint item = QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT g.contentId, g.packId, g.jlptLevel, g.pattern, g.meaningCn, g.formation, g.usageNote, g.furiganaJson,
                         ex.sentenceJp AS exampleSentenceJp, ex.sentenceKana AS exampleSentenceKana,
                         ex.sentenceFuriganaJson AS exampleFuriganaJson, ex.meaningCn AS exampleMeaningCn
                  FROM GrammarPoint g
                  LEFT JOIN GrammarExample ex ON ex.contentId = (
                      SELECT e.contentId FROM GrammarExample e
                      WHERE e.grammarId = g.contentId
                      ORDER BY e.contentId
                      LIMIT 1
                  )
                  WHERE (@jlptLevel IS NULL OR g.jlptLevel = @jlptLevel)
                    AND (@beforeContentId IS NULL OR g.contentId < @beforeContentId)
                  ORDER BY g.contentId DESC
                  LIMIT 1",
                new { jlptLevel = level, beforeContentId = NormalizeContentId(beforeContentId) });

            item = item ?? QueryFirstOrDefault<GrammarPoint>(
                database,
                @"SELECT g.contentId, g.packId, g.jlptLevel, g.pattern, g.meaningCn, g.formation, g.usageNote, g.furiganaJson,
                         ex.sentenceJp AS exampleSentenceJp, ex.sentenceKana AS exampleSentenceKana,
                         ex.sentenceFuriganaJson AS exampleFuriganaJson, ex.meaningCn AS exampleMeaningCn
                  FROM GrammarPoint g
                  LEFT JOIN GrammarExample ex ON ex.contentId = (
                      SELECT e.contentId FROM GrammarExample e
                      WHERE e.grammarId = g.contentId
                      ORDER BY e.contentId
                      LIMIT 1
                  )
                  WHERE (@jlptLevel IS NULL OR g.jlptLevel = @jlptLevel)
                  ORDER BY g.contentId DESC
                  LIMIT 1",
                new { jlptLevel = level });

            PopulateGrammarPointExamples(database, item);
            return item;
        }

        public IReadOnlyList<GrammarExample> GetGrammarExamples(SQLiteConnection database, string jlptLevel, int limit = DefaultLimit)
        {
            EnsureDatabase(database);
            IReadOnlyList<GrammarExample> items = Query<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern,
                         g.meaningCn AS grammarMeaningCn, g.formation AS grammarFormation, g.usageNote AS grammarUsageNote,
                         e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                  ORDER BY e.contentId
                  LIMIT @limit",
                new { jlptLevel = NormalizeLevel(jlptLevel), limit = NormalizeLimit(limit) });
            PopulateGrammarExampleChoiceMeanings(database, items);
            return items;
        }

        public GrammarExample GetNextGrammarExample(SQLiteConnection database, string jlptLevel, string afterContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarExample item = QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern,
                         g.meaningCn AS grammarMeaningCn, g.formation AS grammarFormation, g.usageNote AS grammarUsageNote,
                         e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                    AND (@afterContentId IS NULL OR e.contentId > @afterContentId)
                  ORDER BY e.contentId
                  LIMIT 1",
                new { jlptLevel = level, afterContentId = NormalizeContentId(afterContentId) });

            item = item ?? QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern,
                         g.meaningCn AS grammarMeaningCn, g.formation AS grammarFormation, g.usageNote AS grammarUsageNote,
                         e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                  ORDER BY e.contentId
                  LIMIT 1",
                new { jlptLevel = level });

            PopulateGrammarExampleChoiceMeanings(database, item);
            return item;
        }

        public GrammarExample GetPreviousGrammarExample(SQLiteConnection database, string jlptLevel, string beforeContentId)
        {
            EnsureDatabase(database);
            string level = NormalizeLevel(jlptLevel);
            GrammarExample item = QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern,
                         g.meaningCn AS grammarMeaningCn, g.formation AS grammarFormation, g.usageNote AS grammarUsageNote,
                         e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                    AND (@beforeContentId IS NULL OR e.contentId < @beforeContentId)
                  ORDER BY e.contentId DESC
                  LIMIT 1",
                new { jlptLevel = level, beforeContentId = NormalizeContentId(beforeContentId) });

            item = item ?? QueryFirstOrDefault<GrammarExample>(
                database,
                @"SELECT e.contentId, e.packId, e.grammarId, g.pattern AS grammarPattern,
                         g.meaningCn AS grammarMeaningCn, g.formation AS grammarFormation, g.usageNote AS grammarUsageNote,
                         e.jlptLevel, e.sentenceJp, e.sentenceKana,
                         e.sentenceFuriganaJson, e.meaningCn, e.questionType, e.promptCn, e.correctAnswer, e.distractorsJson
                  FROM GrammarExample e
                  LEFT JOIN GrammarPoint g ON e.grammarId = g.contentId
                  WHERE (@jlptLevel IS NULL OR e.jlptLevel = @jlptLevel)
                  ORDER BY e.contentId DESC
                  LIMIT 1",
                new { jlptLevel = level });

            PopulateGrammarExampleChoiceMeanings(database, item);
            return item;
        }

        private void PopulateGrammarPointExamples(SQLiteConnection database, GrammarPoint item)
        {
            if (item == null)
                return;

            PopulateGrammarPointExamples(database, new[] { item });
        }

        private void PopulateGrammarPointExamples(SQLiteConnection database, IEnumerable<GrammarPoint> items)
        {
            if (items == null)
                return;

            foreach (GrammarPoint item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.contentId))
                    continue;

                List<GrammarExample> examples = new List<GrammarExample>(database.Query<GrammarExample>(
                    @"SELECT sentenceJp, sentenceKana, sentenceFuriganaJson, meaningCn
                      FROM GrammarExample
                      WHERE grammarId = @grammarId
                      ORDER BY contentId
                      LIMIT 2",
                    new { grammarId = item.contentId }));

                if (examples.Count > 0)
                {
                    item.exampleSentenceJp = examples[0].sentenceJp;
                    item.exampleSentenceKana = examples[0].sentenceKana;
                    item.exampleFuriganaJson = examples[0].sentenceFuriganaJson;
                    item.exampleMeaningCn = examples[0].meaningCn;
                }

                if (examples.Count > 1)
                {
                    item.secondExampleSentenceJp = examples[1].sentenceJp;
                    item.secondExampleSentenceKana = examples[1].sentenceKana;
                    item.secondExampleFuriganaJson = examples[1].sentenceFuriganaJson;
                    item.secondExampleMeaningCn = examples[1].meaningCn;
                }
            }
        }

        private void PopulateGrammarExampleChoiceMeanings(SQLiteConnection database, GrammarExample item)
        {
            if (item == null)
                return;

            PopulateGrammarExampleChoiceMeanings(database, new[] { item });
        }

        private void PopulateGrammarExampleChoiceMeanings(SQLiteConnection database, IEnumerable<GrammarExample> items)
        {
            if (items == null)
                return;

            foreach (GrammarExample item in items)
            {
                if (item == null)
                    continue;

                item.sentenceJp = CleanImportedSentence(item.sentenceJp);
                item.correctAnswer = CleanImportedSentence(item.correctAnswer);
                item.distractorsJson = CleanImportedSentence(item.distractorsJson);

                Dictionary<string, string> meanings = new Dictionary<string, string>();
                AddChoiceMeaning(database, meanings, item.sentenceJp, item.meaningCn);
                AddChoiceMeaning(database, meanings, item.correctAnswer, item.meaningCn);

                foreach (string choice in ParseStringList(item.distractorsJson))
                {
                    AddChoiceMeaning(database, meanings, CleanImportedSentence(choice), null);
                }

                item.choiceMeaningsJson = SerializeDictionary(meanings);
            }
        }

        private void AddChoiceMeaning(SQLiteConnection database, IDictionary<string, string> meanings, string sentence, string fallbackMeaning)
        {
            sentence = CleanImportedSentence(sentence);
            if (string.IsNullOrWhiteSpace(sentence) || meanings.ContainsKey(sentence))
                return;

            string meaning = fallbackMeaning;
            if (string.IsNullOrWhiteSpace(meaning))
            {
                meaning = database.QueryFirstOrDefault<string>(
                    @"SELECT meaningCn
                      FROM GrammarExample
                      WHERE sentenceJp = @sentence
                         OR sentenceJp = @dirtySentence
                      ORDER BY contentId
                      LIMIT 1",
                    new
                    {
                        sentence,
                        dirtySentence = RestoreKnownDirtySentence(sentence)
                    });
            }

            if (!string.IsNullOrWhiteSpace(meaning))
                meanings[sentence] = meaning.Trim();
        }

        private List<string> ParseStringList(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<string>));
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return (List<string>)serializer.ReadObject(stream);
                }
            }
            catch
            {
                return new List<string>();
            }
        }

        private string SerializeDictionary(Dictionary<string, string> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, values);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private string CleanImportedSentence(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value
                .Replace("$1BOX", "1BOX")
                .Replace("$5BOX", "5BOX");
        }

        private string RestoreKnownDirtySentence(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value
                .Replace("1BOX", "$1BOX")
                .Replace("5BOX", "$5BOX");
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
