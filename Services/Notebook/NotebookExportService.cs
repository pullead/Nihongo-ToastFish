using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ToastFish.Services.Notebook
{
    public class NotebookExportService
    {
        public void ExportExcel(string filePath, IEnumerable<NotebookItem> vocabulary, IEnumerable<NotebookItem> grammar, IEnumerable<NotebookItem> examples)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ICellStyle headerStyle = CreateHeaderStyle(workbook);
            Dictionary<string, ICellStyle> highlightStyles = CreateHighlightStyles(workbook);

            WriteExcelSheet(workbook, "词汇", VocabularyHeaders(), VocabularyRows(vocabulary), headerStyle, highlightStyles);
            WriteExcelSheet(workbook, "语法", GrammarHeaders(), GrammarRows(grammar), headerStyle, highlightStyles);
            WriteExcelSheet(workbook, "例句", ExampleHeaders(), ExampleRows(examples), headerStyle, highlightStyles);

            using (FileStream stream = File.Create(filePath))
            {
                workbook.Write(stream);
            }
        }

        public void ExportCsv(string filePath, IEnumerable<NotebookItem> vocabulary, IEnumerable<NotebookItem> grammar, IEnumerable<NotebookItem> examples)
        {
            StringBuilder builder = new StringBuilder();
            WriteCsvSection(builder, "词汇", VocabularyHeaders(), VocabularyRows(vocabulary));
            WriteCsvSection(builder, "语法", GrammarHeaders(), GrammarRows(grammar));
            WriteCsvSection(builder, "例句", ExampleHeaders(), ExampleRows(examples));
            File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(true));
        }

        public void ExportPdf(string filePath, IEnumerable<NotebookItem> vocabulary, IEnumerable<NotebookItem> grammar, IEnumerable<NotebookItem> examples)
        {
            PdfDocument document = new PdfDocument();
            document.Info.Title = "Nihongo ToastFish 学习笔记本";

            List<PdfBlock> blocks = new List<PdfBlock>();
            blocks.Add(PdfBlock.Title("Nihongo ToastFish 学习笔记本"));
            AddPdfSection(blocks, "词汇", vocabulary, FormatVocabulary);
            AddPdfSection(blocks, "语法", grammar, FormatGrammar);
            AddPdfSection(blocks, "例句", examples, FormatExample);

            WritePdfPages(document, blocks);
            document.Save(filePath);
        }

        private static void WriteExcelSheet(IWorkbook workbook, string sheetName, string[] headers, IEnumerable<string[]> rows, ICellStyle headerStyle, Dictionary<string, ICellStyle> highlightStyles)
        {
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow header = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                ICell cell = header.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            int rowIndex = 1;
            foreach (string[] rowValues in rows)
            {
                IRow row = sheet.CreateRow(rowIndex++);
                string color = rowValues[rowValues.Length - 1];
                ICellStyle rowStyle = GetCellStyle(highlightStyles, color);

                for (int i = 0; i < rowValues.Length; i++)
                {
                    ICell cell = row.CreateCell(i);
                    cell.SetCellValue(rowValues[i] ?? string.Empty);
                    if (rowStyle != null)
                        cell.CellStyle = rowStyle;
                }
            }

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.SetColumnWidth(i, i == headers.Length - 2 ? 12000 : 4500);
            }
        }

        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;
            IFont font = workbook.CreateFont();
            font.IsBold = true;
            style.SetFont(font);
            return style;
        }

        private static Dictionary<string, ICellStyle> CreateHighlightStyles(IWorkbook workbook)
        {
            return new Dictionary<string, ICellStyle>
            {
                { "Yellow", CreateFillStyle(workbook, IndexedColors.LightYellow.Index) },
                { "Red", CreateFillStyle(workbook, IndexedColors.Rose.Index) },
                { "Green", CreateFillStyle(workbook, IndexedColors.LightGreen.Index) },
                { "Blue", CreateFillStyle(workbook, IndexedColors.LightCornflowerBlue.Index) }
            };
        }

        private static ICellStyle CreateFillStyle(IWorkbook workbook, short color)
        {
            ICellStyle style = workbook.CreateCellStyle();
            style.FillForegroundColor = color;
            style.FillPattern = FillPattern.SolidForeground;
            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Top;
            return style;
        }

        private static ICellStyle GetCellStyle(Dictionary<string, ICellStyle> styles, string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return null;
            return styles.ContainsKey(color) ? styles[color] : null;
        }

        private static void WriteCsvSection(StringBuilder builder, string title, string[] headers, IEnumerable<string[]> rows)
        {
            builder.AppendLine(title);
            builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (string[] row in rows)
            {
                builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
            }
            builder.AppendLine();
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        }

        private static string[] VocabularyHeaders()
        {
            return new[] { "等级", "词汇", "释义/读音", "例句/备注", "添加时间", "颜色标记" };
        }

        private static string[] GrammarHeaders()
        {
            return new[] { "等级", "语法", "释义", "说明", "添加时间", "颜色标记" };
        }

        private static string[] ExampleHeaders()
        {
            return new[] { "等级", "语法", "提示", "例句", "答案", "添加时间", "颜色标记" };
        }

        private static IEnumerable<string[]> VocabularyRows(IEnumerable<NotebookItem> items)
        {
            return (items ?? Enumerable.Empty<NotebookItem>()).Select(item => new[]
            {
                Safe(item.jlptLevel),
                Safe(item.primaryText),
                Safe(item.secondaryText),
                Safe(item.detailText),
                Safe(item.createdAt),
                HighlightLabel(item.highlightColor)
            });
        }

        private static IEnumerable<string[]> GrammarRows(IEnumerable<NotebookItem> items)
        {
            return (items ?? Enumerable.Empty<NotebookItem>()).Select(item => new[]
            {
                Safe(item.jlptLevel),
                Safe(item.primaryText),
                Safe(item.secondaryText),
                Safe(item.detailText),
                Safe(item.createdAt),
                HighlightLabel(item.highlightColor)
            });
        }

        private static IEnumerable<string[]> ExampleRows(IEnumerable<NotebookItem> items)
        {
            return (items ?? Enumerable.Empty<NotebookItem>()).Select(item => new[]
            {
                Safe(item.jlptLevel),
                Safe(item.secondaryText),
                Safe(item.promptText),
                Safe(item.primaryText),
                Safe(item.correctAnswer),
                Safe(item.createdAt),
                HighlightLabel(item.highlightColor)
            });
        }

        private static string FormatVocabulary(NotebookItem item)
        {
            return "词汇：" + Safe(item.primaryText) + "\n释义/读音：" + Safe(item.secondaryText) + "\n例句/备注：" + Safe(item.detailText);
        }

        private static string FormatGrammar(NotebookItem item)
        {
            return "语法：" + Safe(item.primaryText) + "\n释义：" + Safe(item.secondaryText) + "\n说明：" + Safe(item.detailText);
        }

        private static string FormatExample(NotebookItem item)
        {
            return "语法：" + Safe(item.secondaryText) + "\n提示：" + Safe(item.promptText) + "\n例句：" + Safe(item.primaryText) + "\n答案：" + Safe(item.correctAnswer);
        }

        private static string HighlightLabel(string color)
        {
            if (color == "Yellow")
                return "黄色重点";
            if (color == "Red")
                return "红色重点";
            if (color == "Green")
                return "绿色重点";
            if (color == "Blue")
                return "蓝色重点";
            return string.Empty;
        }

        private static string Safe(string value)
        {
            return value ?? string.Empty;
        }

        private static void AddPdfSection(List<PdfBlock> blocks, string sectionTitle, IEnumerable<NotebookItem> items, Func<NotebookItem, string> formatterFunc)
        {
            blocks.Add(PdfBlock.Section(sectionTitle));
            foreach (NotebookItem item in items ?? Enumerable.Empty<NotebookItem>())
            {
                string meta = "等级：" + Safe(item.jlptLevel) + "    标记：" + HighlightLabel(item.highlightColor) + "    添加时间：" + Safe(item.createdAt);
                blocks.Add(PdfBlock.Card(meta, formatterFunc(item), item.highlightColor));
            }
        }

        private static void WritePdfPages(PdfDocument document, List<PdfBlock> blocks)
        {
            const int width = 1600;
            const int height = 1100;
            const int margin = 64;
            const int titleHeight = 58;
            const int sectionHeight = 42;
            const int cardHeight = 148;

            int index = 0;
            while (index < blocks.Count)
            {
                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (Font titleFont = new Font("Microsoft YaHei UI", 25, FontStyle.Bold))
                using (Font sectionFont = new Font("Microsoft YaHei UI", 19, FontStyle.Bold))
                using (Font bodyFont = new Font("Microsoft YaHei UI", 14, FontStyle.Regular))
                using (Font metaFont = new Font("Microsoft YaHei UI", 12, FontStyle.Regular))
                using (StringFormat stringFormat = new StringFormat())
                {
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    graphics.Clear(Color.White);
                    stringFormat.Trimming = StringTrimming.EllipsisWord;
                    stringFormat.FormatFlags = 0;

                    int y = margin;
                    while (index < blocks.Count)
                    {
                        PdfBlock block = blocks[index];
                        int blockHeight = block.Kind == PdfBlockKind.Title ? titleHeight : block.Kind == PdfBlockKind.Section ? sectionHeight : cardHeight;
                        if (y + blockHeight > height - margin && y > margin)
                            break;

                        DrawPdfBlock(graphics, block, margin, y, width - margin * 2, blockHeight, titleFont, sectionFont, bodyFont, metaFont, stringFormat);
                        y += blockHeight;
                        index++;
                    }

                    AddBitmapPage(document, bitmap);
                }
            }
        }

        private static void DrawPdfBlock(Graphics graphics, PdfBlock block, int x, int y, int width, int height, Font titleFont, Font sectionFont, Font bodyFont, Font metaFont, StringFormat stringFormat)
        {
            if (block.Kind == PdfBlockKind.Title)
            {
                graphics.DrawString(block.Text, titleFont, Brushes.Black, new RectangleF(x, y, width, height), stringFormat);
                return;
            }

            if (block.Kind == PdfBlockKind.Section)
            {
                graphics.DrawString(block.Text, sectionFont, Brushes.Black, new RectangleF(x, y + 4, width, height), stringFormat);
                return;
            }

            using (Brush background = new SolidBrush(GetPdfColor(block.HighlightColor)))
            using (Pen border = new Pen(Color.FromArgb(210, 210, 210)))
            {
                Rectangle rect = new Rectangle(x, y, width, height - 12);
                graphics.FillRectangle(background, rect);
                graphics.DrawRectangle(border, rect);
                graphics.DrawString(block.Meta, metaFont, Brushes.DimGray, new RectangleF(x + 12, y + 10, width - 24, 24), stringFormat);
                graphics.DrawString(block.Text, bodyFont, Brushes.Black, new RectangleF(x + 12, y + 38, width - 24, height - 54), stringFormat);
            }
        }

        private static void AddBitmapPage(PdfDocument document, Bitmap bitmap)
        {
            PdfPage page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Landscape;

            string imagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
            try
            {
                bitmap.Save(imagePath, ImageFormat.Png);
                using (XGraphics graphics = XGraphics.FromPdfPage(page))
                using (XImage image = XImage.FromFile(imagePath))
                {
                    graphics.DrawImage(image, 0, 0, page.Width, page.Height);
                }
            }
            finally
            {
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }
        }

        private static Color GetPdfColor(string color)
        {
            if (color == "Yellow")
                return Color.FromArgb(255, 255, 242, 184);
            if (color == "Red")
                return Color.FromArgb(255, 255, 217, 217);
            if (color == "Green")
                return Color.FromArgb(255, 223, 245, 223);
            if (color == "Blue")
                return Color.FromArgb(255, 221, 235, 255);
            return Color.White;
        }

        private enum PdfBlockKind
        {
            Title,
            Section,
            Card
        }

        private class PdfBlock
        {
            public PdfBlockKind Kind { get; private set; }
            public string Text { get; private set; }
            public string Meta { get; private set; }
            public string HighlightColor { get; private set; }

            public static PdfBlock Title(string text)
            {
                return new PdfBlock { Kind = PdfBlockKind.Title, Text = text };
            }

            public static PdfBlock Section(string text)
            {
                return new PdfBlock { Kind = PdfBlockKind.Section, Text = text };
            }

            public static PdfBlock Card(string meta, string text, string highlightColor)
            {
                return new PdfBlock { Kind = PdfBlockKind.Card, Meta = meta, Text = text, HighlightColor = highlightColor };
            }
        }
    }
}
