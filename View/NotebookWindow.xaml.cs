using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ToastFish.Services.Notebook;

namespace ToastFish
{
    public partial class NotebookWindow : Window
    {
        private readonly SQLiteConnection database;
        private readonly NotebookService notebookService = new NotebookService();
        private readonly NotebookExportService exportService = new NotebookExportService();

        public NotebookWindow(SQLiteConnection database)
        {
            this.database = database;
            InitializeComponent();
            ConfigureNotebookGrid(VocabularyGrid);
            ConfigureNotebookGrid(GrammarGrid);
            ConfigureNotebookGrid(ExampleGrid);
            LoadNotebook();
        }

        private void LoadNotebook()
        {
            VocabularyGrid.ItemsSource = notebookService.GetItems(database, "Vocabulary");
            GrammarGrid.ItemsSource = notebookService.GetItems(database, "Grammar");
            ExampleGrid.ItemsSource = notebookService.GetItems(database, "Example");
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "导出学习笔记本",
                FileName = "Nihongo-ToastFish-学习笔记本",
                Filter = "Excel 工作簿 (*.xlsx)|*.xlsx|CSV 文件 (*.csv)|*.csv|PDF 文件 (*.pdf)|*.pdf",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                IReadOnlyList<NotebookItem> vocabulary = notebookService.GetItems(database, "Vocabulary");
                IReadOnlyList<NotebookItem> grammar = notebookService.GetItems(database, "Grammar");
                IReadOnlyList<NotebookItem> examples = notebookService.GetItems(database, "Example");

                string extension = System.IO.Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (extension == ".xlsx")
                {
                    exportService.ExportExcel(dialog.FileName, vocabulary, grammar, examples);
                }
                else if (extension == ".csv")
                {
                    exportService.ExportCsv(dialog.FileName, vocabulary, grammar, examples);
                }
                else if (extension == ".pdf")
                {
                    exportService.ExportPdf(dialog.FileName, vocabulary, grammar, examples);
                }
                else
                {
                    MessageBox.Show(this, "请选择 .xlsx、.csv 或 .pdf 文件。", "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show(this, "笔记本导出完成。", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this, "导出失败：" + ex.Message, "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureNotebookGrid(DataGrid grid)
        {
            grid.ContextMenu = BuildContextMenu();
            grid.PreviewMouseRightButtonDown += NotebookGrid_PreviewMouseRightButtonDown;
        }

        private ContextMenu BuildContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            MenuItem reviewed = new MenuItem { Header = "已经复习" };
            reviewed.Click += Reviewed_Click;
            menu.Items.Add(reviewed);

            MenuItem delete = new MenuItem { Header = "删除" };
            delete.Click += Delete_Click;
            menu.Items.Add(delete);

            MenuItem highlight = new MenuItem { Header = "用颜色重点标记" };
            highlight.Items.Add(BuildHighlightMenuItem("黄色重点", "Yellow"));
            highlight.Items.Add(BuildHighlightMenuItem("红色重点", "Red"));
            highlight.Items.Add(BuildHighlightMenuItem("绿色重点", "Green"));
            highlight.Items.Add(BuildHighlightMenuItem("蓝色重点", "Blue"));
            highlight.Items.Add(BuildHighlightMenuItem("取消颜色标记", null));
            menu.Items.Add(highlight);

            menu.Opened += ContextMenu_Opened;
            return menu;
        }

        private MenuItem BuildHighlightMenuItem(string header, string color)
        {
            MenuItem item = new MenuItem { Header = header, Tag = color };
            item.Click += Highlight_Click;
            return item;
        }

        private void NotebookGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid grid = sender as DataGrid;
            DataGridRow row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (grid == null || row == null)
                return;

            if (!row.IsSelected)
            {
                grid.SelectedItems.Clear();
                row.IsSelected = true;
            }

            row.Focus();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            DataGrid grid = menu == null ? null : menu.PlacementTarget as DataGrid;
            bool hasSelection = GetSelectedItems(grid).Count > 0;

            foreach (object item in menu.Items)
            {
                MenuItem menuItem = item as MenuItem;
                if (menuItem != null)
                    menuItem.IsEnabled = hasSelection;
            }
        }

        private void Reviewed_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItems(sender);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItems(sender);
        }

        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
                return;

            DataGrid grid = GetContextMenuGrid(menuItem);
            List<long> ids = GetSelectedItemIds(grid);
            if (ids.Count == 0)
                return;

            notebookService.SetHighlightColor(database, ids, menuItem.Tag as string);
            LoadNotebook();
        }

        private void DeleteSelectedItems(object sender)
        {
            DataGrid grid = GetContextMenuGrid(sender as DependencyObject);
            List<long> ids = GetSelectedItemIds(grid);
            if (ids.Count == 0)
                return;

            notebookService.DeleteItems(database, ids);
            LoadNotebook();
        }

        private DataGrid GetContextMenuGrid(DependencyObject source)
        {
            DependencyObject current = source;
            while (current != null)
            {
                ContextMenu menu = current as ContextMenu;
                if (menu != null)
                    return menu.PlacementTarget as DataGrid;

                current = LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        private List<NotebookItem> GetSelectedItems(DataGrid grid)
        {
            if (grid == null)
                return new List<NotebookItem>();

            return grid.SelectedItems
                .OfType<NotebookItem>()
                .ToList();
        }

        private List<long> GetSelectedItemIds(DataGrid grid)
        {
            return GetSelectedItems(grid)
                .Select(item => item.id)
                .ToList();
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                T parent = child as T;
                if (parent != null)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }
    }
}
