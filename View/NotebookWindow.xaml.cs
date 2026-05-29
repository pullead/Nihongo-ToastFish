using System.Data.SQLite;
using System.Windows;
using ToastFish.Services.Notebook;

namespace ToastFish
{
    public partial class NotebookWindow : Window
    {
        private readonly SQLiteConnection database;
        private readonly NotebookService notebookService = new NotebookService();

        public NotebookWindow(SQLiteConnection database)
        {
            this.database = database;
            InitializeComponent();
            LoadNotebook();
        }

        private void LoadNotebook()
        {
            VocabularyGrid.ItemsSource = notebookService.GetItems(database, "Vocabulary");
            GrammarGrid.ItemsSource = notebookService.GetItems(database, "Grammar");
            ExampleGrid.ItemsSource = notebookService.GetItems(database, "Example");
        }
    }
}
