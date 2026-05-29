using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ToastFish.Services.Content;
using ToastFish.Services.ContentUpdate;
using ToastFish.Services.Notebook;
using ToastFish.Services.Notifications;

namespace ToastFish.Services.Study
{
    public class ImportedContentStudyService
    {
        private readonly ContentPackImporter importer;
        private readonly ContentRepository repository;
        private readonly StudyCardFactory cardFactory;
        private readonly NotificationService notificationService;
        private readonly StudySessionStateService stateService;
        private readonly NotebookService notebookService;
        private readonly IObservable<string> navigationHotKeys;
        private readonly Func<string, string> navigationHotKeyMapper;

        public ImportedContentStudyService()
            : this(
                  new ContentPackImporter(),
                  new ContentRepository(),
                  new StudyCardFactory(),
                  new NotificationService(),
                  new StudySessionStateService(),
                  new NotebookService())
        {
        }

        public ImportedContentStudyService(
            ContentPackImporter importer,
            ContentRepository repository,
            StudyCardFactory cardFactory,
            NotificationService notificationService,
            StudySessionStateService stateService,
            NotebookService notebookService,
            IObservable<string> navigationHotKeys = null,
            Func<string, string> navigationHotKeyMapper = null)
        {
            this.importer = importer ?? new ContentPackImporter();
            this.repository = repository ?? new ContentRepository();
            this.cardFactory = cardFactory ?? new StudyCardFactory();
            this.notificationService = notificationService ?? new NotificationService();
            this.stateService = stateService ?? new StudySessionStateService();
            this.notebookService = notebookService ?? new NotebookService();
            this.navigationHotKeys = navigationHotKeys;
            this.navigationHotKeyMapper = navigationHotKeyMapper;
        }

        public StudyCard GetFirstVocabularyCard(SQLiteConnection database, string jlptLevel = "N5")
        {
            return GetFirstCard(database, ImportedContentStudyMode.Vocabulary, jlptLevel);
        }

        public StudyCard GetFirstCard(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel = "N5")
        {
            return GetNextCard(database, mode, jlptLevel, null);
        }

        public void ShowFirstVocabularyCard(SQLiteConnection database, string jlptLevel = "N5")
        {
            ShowFirstCard(database, ImportedContentStudyMode.Vocabulary, jlptLevel);
        }

        public void ShowFirstCard(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel = "N5")
        {
            RunFirstStudyAsync(database, mode, jlptLevel, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        public void ShowNextSavedCard(SQLiteConnection database)
        {
            RunSavedStudyAsync(database, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        public Task RunFirstStudyAsync(
            SQLiteConnection database,
            ImportedContentStudyMode mode,
            string jlptLevel,
            CancellationToken cancellationToken)
        {
            return RunStudyAsync(database, mode, jlptLevel, null, cancellationToken);
        }

        public Task RunFirstGrammarPracticeAsync(
            SQLiteConnection database,
            string jlptLevel,
            CancellationToken cancellationToken)
        {
            return RunGrammarPracticeAsync(database, jlptLevel, null, cancellationToken);
        }

        public Task RunSavedStudyAsync(SQLiteConnection database, CancellationToken cancellationToken)
        {
            StudySessionState state = stateService.GetLastStudy(database);
            ImportedContentStudyMode mode = ParseMode(state.contentKind);
            string level = string.IsNullOrWhiteSpace(state.jlptLevel) ? "N5" : state.jlptLevel;
            return RunStudyAsync(database, mode, level, state.lastContentId, cancellationToken);
        }

        public void ShowNextCard(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel, string afterContentId)
        {
            StudyCard card = GetNextCard(database, mode, jlptLevel, afterContentId);
            if (card == null)
            {
                notificationService.ShowMessage("还没有可学习的内置日语内容。");
                return;
            }

            stateService.SaveBuiltin(database, mode, card.JlptLevel ?? jlptLevel, card.ContentId);
            notificationService.ShowStudyCard(card, "已看");
        }

        public string GetSavedStudyStatusMessage(SQLiteConnection database)
        {
            EnsureBundledContentImported(database);
            StudySessionState state = stateService.GetLastStudy(database);
            ImportedContentStudyMode mode = ParseMode(state.contentKind);
            string level = string.IsNullOrWhiteSpace(state.jlptLevel) ? "N5" : state.jlptLevel;
            int total = CountItems(database, mode, level, null);
            int current = CountItems(database, mode, level, state.lastContentId);

            return "上次学习：" + level + " " + GetModeDisplayName(mode) + "\n" +
                   "当前进度：" + current + "/" + total + "\n" +
                   "右键托盘或按 Alt+Q 继续学习";
        }

        private async Task RunStudyAsync(
            SQLiteConnection database,
            ImportedContentStudyMode mode,
            string jlptLevel,
            string afterContentId,
            CancellationToken cancellationToken)
        {
            StudyCard card = GetNextCard(database, mode, jlptLevel, afterContentId);
            if (card == null)
            {
                notificationService.ShowMessage("还没有可学习的内置日语内容。");
                return;
            }

            while (!cancellationToken.IsCancellationRequested && card != null)
            {
                stateService.SaveBuiltin(database, mode, card.JlptLevel ?? jlptLevel, card.ContentId);
                string action = await notificationService.ShowStudyCardNavigationAndWaitAsync(
                    card,
                    navigationHotKeys,
                    navigationHotKeyMapper,
                    cancellationToken);

                if (action == NotificationAction.Cancel || cancellationToken.IsCancellationRequested)
                    break;

                if (action == NotificationAction.AddNote)
                {
                    notebookService.AddCard(database, card);
                    card = GetNextCard(database, mode, card.JlptLevel ?? jlptLevel, card.ContentId);
                    continue;
                }

                if (action == NotificationAction.Details)
                {
                    ShowDetailsWindow(card);
                    continue;
                }

                if (action == NotificationAction.Previous)
                    card = GetPreviousCard(database, mode, card.JlptLevel ?? jlptLevel, card.ContentId);
                else
                    card = GetNextCard(database, mode, card.JlptLevel ?? jlptLevel, card.ContentId);
            }
        }

        private void ShowDetailsWindow(StudyCard card)
        {
            if (Application.Current == null)
                return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                global::ToastFish.StudyCardDetailWindow window = new global::ToastFish.StudyCardDetailWindow(card);
                window.Show();
                window.Activate();
            }));
        }

        private async Task RunGrammarPracticeAsync(
            SQLiteConnection database,
            string jlptLevel,
            string afterContentId,
            CancellationToken cancellationToken)
        {
            StudyCard card = GetNextCard(database, ImportedContentStudyMode.Example, jlptLevel, afterContentId);
            if (card == null)
            {
                notificationService.ShowMessage("还没有可练习的内置日语语法内容。");
                return;
            }

            while (!cancellationToken.IsCancellationRequested && card != null)
            {
                PracticeQuestion question = CreatePracticeQuestion(card);
                stateService.SaveBuiltin(database, ImportedContentStudyMode.Example, card.JlptLevel ?? jlptLevel, card.ContentId);
                string action = await notificationService.ShowQuestionAndWaitAsync(
                    "日语语法练习",
                    question.Prompt,
                    question.Buttons,
                    navigationHotKeys,
                    navigationHotKeyMapper,
                    cancellationToken);

                if (action == NotificationAction.Cancel || cancellationToken.IsCancellationRequested)
                    break;

                if (action == NotificationAction.Previous)
                {
                    card = GetPreviousCard(database, ImportedContentStudyMode.Example, card.JlptLevel ?? jlptLevel, card.ContentId);
                    continue;
                }

                if (action == NotificationAction.Next)
                {
                    card = GetNextCard(database, ImportedContentStudyMode.Example, card.JlptLevel ?? jlptLevel, card.ContentId);
                    continue;
                }

                int selectedIndex;
                if (int.TryParse(action, out selectedIndex) && selectedIndex == question.CorrectIndex)
                {
                    card = GetNextCard(database, ImportedContentStudyMode.Example, card.JlptLevel ?? jlptLevel, card.ContentId);
                }
                else
                {
                    notificationService.ShowMessage("错误，正确答案：" + question.CorrectLabel);
                    await Task.Delay(1200);
                }
            }
        }

        private PracticeQuestion CreatePracticeQuestion(StudyCard card)
        {
            List<string> choices = new List<string>(card.Choices ?? new List<string>());
            if (!string.IsNullOrWhiteSpace(card.CorrectAnswer) && !choices.Contains(card.CorrectAnswer))
                choices.Insert(0, card.CorrectAnswer);

            LimitChoicesForToast(choices, card.CorrectAnswer, 3);
            Shuffle(choices);
            int correctIndex = choices.IndexOf(card.CorrectAnswer);
            if (correctIndex < 0)
                correctIndex = 0;

            string prompt = JoinLines(card.SecondaryText, card.PromptText, card.PrimaryText);
            List<string> buttons = new List<string>();
            for (int index = 0; index < choices.Count; index++)
            {
                buttons.Add(((char)('A' + index)) + "." + choices[index]);
            }

            return new PracticeQuestion
            {
                Prompt = prompt,
                Buttons = buttons,
                CorrectIndex = correctIndex,
                CorrectLabel = ((char)('A' + correctIndex)) + "." + choices[correctIndex]
            };
        }

        private void LimitChoicesForToast(List<string> choices, string correctAnswer, int maxCount)
        {
            if (choices == null || choices.Count <= maxCount)
                return;

            List<string> limited = new List<string>();
            if (!string.IsNullOrWhiteSpace(correctAnswer))
                limited.Add(correctAnswer);

            foreach (string choice in choices)
            {
                if (limited.Count >= maxCount)
                    break;
                if (string.IsNullOrWhiteSpace(choice) || limited.Contains(choice))
                    continue;

                limited.Add(choice);
            }

            choices.Clear();
            choices.AddRange(limited);
        }

        private void Shuffle<T>(IList<T> values)
        {
            if (values == null || values.Count < 2)
                return;

            Random random = new Random();
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                T value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private StudyCard GetNextCard(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel, string afterContentId)
        {
            EnsureBundledContentImported(database);

            switch (mode)
            {
                case ImportedContentStudyMode.Vocabulary:
                    return cardFactory.FromVocabulary(repository.GetNextVocabularyItem(database, jlptLevel, afterContentId));
                case ImportedContentStudyMode.Grammar:
                    return cardFactory.FromGrammarPoint(repository.GetNextGrammarPoint(database, jlptLevel, afterContentId));
                case ImportedContentStudyMode.Example:
                    return cardFactory.FromGrammarExample(repository.GetNextGrammarExample(database, jlptLevel, afterContentId));
                default:
                    return null;
            }
        }

        private StudyCard GetPreviousCard(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel, string beforeContentId)
        {
            EnsureBundledContentImported(database);

            switch (mode)
            {
                case ImportedContentStudyMode.Vocabulary:
                    return cardFactory.FromVocabulary(repository.GetPreviousVocabularyItem(database, jlptLevel, beforeContentId));
                case ImportedContentStudyMode.Grammar:
                    return cardFactory.FromGrammarPoint(repository.GetPreviousGrammarPoint(database, jlptLevel, beforeContentId));
                case ImportedContentStudyMode.Example:
                    return cardFactory.FromGrammarExample(repository.GetPreviousGrammarExample(database, jlptLevel, beforeContentId));
                default:
                    return null;
            }
        }

        private int CountItems(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel, string throughContentId)
        {
            string tableName;
            string idColumn;
            string levelColumn;
            switch (mode)
            {
                case ImportedContentStudyMode.Grammar:
                    tableName = "GrammarPoint";
                    idColumn = "contentId";
                    levelColumn = "jlptLevel";
                    break;
                case ImportedContentStudyMode.Example:
                    tableName = "GrammarExample";
                    idColumn = "contentId";
                    levelColumn = "jlptLevel";
                    break;
                default:
                    tableName = "VocabularyItem";
                    idColumn = "contentId";
                    levelColumn = "jlptLevel";
                    break;
            }

            using (SQLiteCommand command = database.CreateCommand())
            {
                command.CommandText =
                    "SELECT COUNT(*) FROM " + tableName +
                    " WHERE (@jlptLevel IS NULL OR " + levelColumn + " = @jlptLevel)" +
                    " AND (@throughContentId IS NULL OR " + idColumn + " <= @throughContentId)";
                command.Parameters.AddWithValue("@jlptLevel", string.IsNullOrWhiteSpace(jlptLevel) ? (object)DBNull.Value : jlptLevel);
                command.Parameters.AddWithValue("@throughContentId", string.IsNullOrWhiteSpace(throughContentId) ? (object)DBNull.Value : throughContentId);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private string GetModeDisplayName(ImportedContentStudyMode mode)
        {
            switch (mode)
            {
                case ImportedContentStudyMode.Grammar:
                    return "语法";
                case ImportedContentStudyMode.Example:
                    return "例句";
                default:
                    return "词汇";
            }
        }

        private void EnsureBundledContentImported(SQLiteConnection database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            string manifestPath = GetBundledContentManifestPath();
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Bundled content manifest was not found.", manifestPath);

            if (HasInstalledManifestPacks(database, manifestPath))
                return;

            importer.ImportManifest(manifestPath, database);
        }

        private bool HasInstalledManifestPacks(SQLiteConnection database, string manifestPath)
        {
            string manifestText = File.ReadAllText(manifestPath);
            int expectedPackCount = CountOccurrences(manifestText, "\"packId\"");
            if (expectedPackCount == 0)
                return false;

            string manifestFileName = Path.GetFileName(manifestPath);
            string sql;
            if (string.Equals(manifestFileName, "manifest-builtin-jlpt.json", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"SELECT COUNT(*)
                        FROM ContentPack
                        WHERE packId LIKE 'builtin-gojuon-%'
                           OR packId LIKE 'builtin-vocabulary-%'
                           OR packId LIKE 'builtin-grammar-%'
                           OR packId LIKE 'builtin-examples-%'";
            }
            else
            {
                sql = @"SELECT COUNT(*)
                        FROM ContentPack
                        WHERE packId LIKE 'builtin-%-smoke'";
            }

            using (SQLiteCommand command = database.CreateCommand())
            {
                command.CommandText = sql;
                object value = command.ExecuteScalar();
                int installedPackCount = Convert.ToInt32(value);
                return installedPackCount >= expectedPackCount;
            }
        }

        private ImportedContentStudyMode ParseMode(string contentKind)
        {
            ImportedContentStudyMode mode;
            if (Enum.TryParse(contentKind, out mode))
                return mode;

            return ImportedContentStudyMode.Vocabulary;
        }

        private int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while (!string.IsNullOrEmpty(text) && (index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private string JoinLines(params string[] lines)
        {
            List<string> values = new List<string>();
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    values.Add(line.Trim());
            }

            return string.Join("\n", values);
        }

        private string GetBundledContentManifestPath()
        {
            string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string executableDirectory = Path.GetDirectoryName(executablePath);
            string contentDirectory = Path.Combine(executableDirectory, "Resources", "Content");
            string builtinManifestPath = Path.Combine(contentDirectory, "manifest-builtin-jlpt.json");
            if (File.Exists(builtinManifestPath))
                return builtinManifestPath;

            return Path.Combine(contentDirectory, "manifest-smoke.json");
        }

        private class PracticeQuestion
        {
            public string Prompt { get; set; }
            public List<string> Buttons { get; set; }
            public int CorrectIndex { get; set; }
            public string CorrectLabel { get; set; }
        }
    }
}
