using System;
using System.Data.SQLite;
using System.IO;
using ToastFish.Services.Content;
using ToastFish.Services.ContentUpdate;
using ToastFish.Services.Notifications;

namespace ToastFish.Services.Study
{
    public class ImportedContentStudyService
    {
        private readonly ContentPackImporter importer;
        private readonly ContentRepository repository;
        private readonly StudyCardFactory cardFactory;
        private readonly NotificationService notificationService;

        public ImportedContentStudyService()
            : this(
                  new ContentPackImporter(),
                  new ContentRepository(),
                  new StudyCardFactory(),
                  new NotificationService())
        {
        }

        public ImportedContentStudyService(
            ContentPackImporter importer,
            ContentRepository repository,
            StudyCardFactory cardFactory,
            NotificationService notificationService)
        {
            this.importer = importer ?? new ContentPackImporter();
            this.repository = repository ?? new ContentRepository();
            this.cardFactory = cardFactory ?? new StudyCardFactory();
            this.notificationService = notificationService ?? new NotificationService();
        }

        public StudyCard GetFirstVocabularyCard(SQLiteConnection database, string jlptLevel = "N5")
        {
            return GetFirstCard(database, ImportedContentStudyMode.Vocabulary, jlptLevel);
        }

        public StudyCard GetFirstCard(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel = "N5")
        {
            EnsureSmokeContentImported(database);

            switch (mode)
            {
                case ImportedContentStudyMode.Vocabulary:
                    var vocabularyItems = repository.GetVocabularyItems(database, jlptLevel, 1);
                    return vocabularyItems.Count == 0 ? null : cardFactory.FromVocabulary(vocabularyItems[0]);
                case ImportedContentStudyMode.Grammar:
                    var grammarPoints = repository.GetGrammarPoints(database, jlptLevel, 1);
                    return grammarPoints.Count == 0 ? null : cardFactory.FromGrammarPoint(grammarPoints[0]);
                case ImportedContentStudyMode.Example:
                    var grammarExamples = repository.GetGrammarExamples(database, jlptLevel, 1);
                    return grammarExamples.Count == 0 ? null : cardFactory.FromGrammarExample(grammarExamples[0]);
                default:
                    return null;
            }
        }

        public void ShowFirstVocabularyCard(SQLiteConnection database, string jlptLevel = "N5")
        {
            ShowFirstCard(database, ImportedContentStudyMode.Vocabulary, jlptLevel);
        }

        public void ShowFirstCard(SQLiteConnection database, ImportedContentStudyMode mode, string jlptLevel = "N5")
        {
            StudyCard card = GetFirstCard(database, mode, jlptLevel);
            if (card == null)
            {
                notificationService.ShowMessage("还没有可学习的内置日语内容。");
                return;
            }

            notificationService.ShowStudyCard(card, "已看");
        }

        private void EnsureSmokeContentImported(SQLiteConnection database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var vocabularyItems = repository.GetVocabularyItems(database, "N5", 1);
            if (vocabularyItems.Count > 0)
                return;

            string manifestPath = GetBundledContentManifestPath();
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Bundled content manifest was not found.", manifestPath);

            importer.ImportManifest(manifestPath, database);
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
    }
}
