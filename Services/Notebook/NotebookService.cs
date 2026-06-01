using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dapper;
using ToastFish.Services.Study;

namespace ToastFish.Services.Notebook
{
    public class NotebookService
    {
        public bool AddCard(SQLiteConnection database, StudyCard card)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (card == null || string.IsNullOrWhiteSpace(card.ContentId))
                return false;

            int changed = database.Execute(
                @"INSERT OR IGNORE INTO StudyNotebookItem
                    (contentKind, jlptLevel, contentId, title, primaryText, secondaryText, detailText, promptText, correctAnswer, createdAt)
                  VALUES
                    (@contentKind, @jlptLevel, @contentId, @title, @primaryText, @secondaryText, @detailText, @promptText, @correctAnswer, @createdAt)",
                new
                {
                    contentKind = card.Kind.ToString(),
                    jlptLevel = card.JlptLevel,
                    contentId = card.ContentId,
                    title = card.Title,
                    primaryText = card.PrimaryText,
                    secondaryText = card.SecondaryText,
                    detailText = card.DetailText,
                    promptText = card.PromptText,
                    correctAnswer = card.CorrectAnswer,
                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

            return changed > 0;
        }

        public IReadOnlyList<NotebookItem> GetItems(SQLiteConnection database, string contentKind)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            return new List<NotebookItem>(database.Query<NotebookItem>(
                @"SELECT id, contentKind, jlptLevel, contentId, title, primaryText, secondaryText,
                         detailText, promptText, correctAnswer, createdAt, highlightColor
                  FROM StudyNotebookItem
                  WHERE contentKind = @contentKind
                  ORDER BY createdAt DESC, id DESC",
                new { contentKind = contentKind }));
        }

        public void DeleteItems(SQLiteConnection database, IEnumerable<long> ids)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (ids == null)
                return;

            database.Execute(
                "DELETE FROM StudyNotebookItem WHERE id IN @ids",
                new { ids = ids });
        }

        public void SetHighlightColor(SQLiteConnection database, IEnumerable<long> ids, string color)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (ids == null)
                return;

            database.Execute(
                "UPDATE StudyNotebookItem SET highlightColor = @color WHERE id IN @ids",
                new { color = color, ids = ids });
        }
    }
}
