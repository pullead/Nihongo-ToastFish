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
            EnsureSmokeContentImported(database);
            var vocabularyItems = repository.GetVocabularyItems(database, jlptLevel, 1);
            if (vocabularyItems.Count == 0)
                return null;

            return cardFactory.FromVocabulary(vocabularyItems[0]);
        }

        public void ShowFirstVocabularyCard(SQLiteConnection database, string jlptLevel = "N5")
        {
            StudyCard card = GetFirstVocabularyCard(database, jlptLevel);
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

            string manifestPath = GetBundledSmokeManifestPath();
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Bundled smoke content manifest was not found.", manifestPath);

            importer.ImportManifest(manifestPath, database);
        }

        private string GetBundledSmokeManifestPath()
        {
            string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string executableDirectory = Path.GetDirectoryName(executablePath);
            return Path.Combine(executableDirectory, "Resources", "Content", "manifest-smoke.json");
        }
    }
}
