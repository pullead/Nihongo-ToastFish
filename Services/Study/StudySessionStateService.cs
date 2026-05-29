using System;
using System.Data.SQLite;
using Dapper;

namespace ToastFish.Services.Study
{
    public class StudySessionStateService
    {
        public const string LastStudyStateKey = "last-study";
        public const string SourceBuiltin = "builtin";
        public const string SourceLegacy = "legacy";

        public StudySessionState GetLastStudy(SQLiteConnection database)
        {
            EnsureDatabase(database);
            StudySessionState state = database.QueryFirstOrDefault<StudySessionState>(
                @"SELECT stateKey, studySource, legacyTableName, contentKind, jlptLevel, lastContentId, updatedAt
                  FROM StudySessionState
                  WHERE stateKey = @stateKey",
                new { stateKey = LastStudyStateKey });

            if (state == null || state.studySource == SourceLegacy)
                return CreateDefaultBuiltinState();

            return state;
        }

        public void SaveBuiltin(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel, string contentId)
        {
            EnsureDatabase(database);
            Upsert(
                database,
                SourceBuiltin,
                null,
                mode.ToString(),
                NormalizeLevel(jlptLevel),
                contentId);
        }

        public void SaveLegacy(SQLiteConnection database, string legacyTableName)
        {
            EnsureDatabase(database);
            // Legacy English/custom selection is no longer the primary resume target.
            // Keep the last built-in Japanese study state intact across app restarts.
        }

        public StudySessionState CreateDefaultBuiltinState()
        {
            return new StudySessionState
            {
                stateKey = LastStudyStateKey,
                studySource = SourceBuiltin,
                contentKind = ImportedContentStudyMode.Vocabulary.ToString(),
                jlptLevel = "N5",
                lastContentId = null,
                updatedAt = DateTime.UtcNow.ToString("o")
            };
        }

        private void Upsert(
            SQLiteConnection database,
            string source,
            string legacyTableName,
            string contentKind,
            string jlptLevel,
            string lastContentId)
        {
            database.Execute(
                @"INSERT OR REPLACE INTO StudySessionState
                    (stateKey, studySource, legacyTableName, contentKind, jlptLevel, lastContentId, updatedAt)
                  VALUES
                    (@stateKey, @studySource, @legacyTableName, @contentKind, @jlptLevel, @lastContentId, @updatedAt)",
                new
                {
                    stateKey = LastStudyStateKey,
                    studySource = source,
                    legacyTableName = legacyTableName,
                    contentKind = contentKind,
                    jlptLevel = jlptLevel,
                    lastContentId = lastContentId,
                    updatedAt = DateTime.UtcNow.ToString("o")
                });
        }

        private string NormalizeLevel(string jlptLevel)
        {
            return string.IsNullOrWhiteSpace(jlptLevel) ? "N5" : jlptLevel.Trim().ToUpperInvariant();
        }

        private void EnsureDatabase(SQLiteConnection database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
        }
    }
}
