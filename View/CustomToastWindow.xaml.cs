using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ToastFish
{
    public partial class CustomToastWindow : Window
    {
        private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(222, 222, 222));
        private bool actionSelected;
        private readonly IList<string> highlightTerms;

        public event Action<string> ActionSelected;

        public CustomToastWindow(string message, IList<Tuple<string, string>> buttons)
            : this(message, buttons, null)
        {
        }

        public CustomToastWindow(string message, IList<Tuple<string, string>> buttons, string highlightText)
        {
            highlightTerms = CreateHighlightTerms(highlightText);
            InitializeComponent();
            SetFormattedMessage(message);
            AddButtons(buttons);
        }

        private void SetFormattedMessage(string message)
        {
            MessageText.Inlines.Clear();

            string[] lines = (message ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            bool emphasizedFirstContentLine = false;
            for (int index = 0; index < lines.Length; index++)
            {
                AddFormattedLine(lines[index], ref emphasizedFirstContentLine);
                if (index < lines.Length - 1)
                    MessageText.Inlines.Add(new LineBreak());
            }
        }

        private void AddFormattedLine(string line, ref bool emphasizedFirstContentLine)
        {
            string label;
            string value;

            if (!emphasizedFirstContentLine && !string.IsNullOrWhiteSpace(line))
            {
                MessageText.Inlines.Add(new Run(line)
                {
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    Background = HasHighlightTerms() ? HighlightBrush : null
                });
                emphasizedFirstContentLine = true;
                return;
            }

            if (TrySplitChoice(line, out label, out value))
            {
                MessageText.Inlines.Add(new Run(label)
                {
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black
                });
                AddTextWithHighlight(value);
                return;
            }

            if (TrySplitLabel(line, out label, out value))
            {
                MessageText.Inlines.Add(new Run(label)
                {
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black
                });
                AddTextWithHighlight(value);
                return;
            }

            AddTextWithHighlight(line);
        }

        private void AddTextWithHighlight(string text)
        {
            if (string.IsNullOrEmpty(text) || !HasHighlightTerms())
            {
                MessageText.Inlines.Add(new Run(text ?? string.Empty));
                return;
            }

            int index = 0;
            while (index < text.Length)
            {
                int matchIndex;
                string matchTerm = FindNextHighlight(text, index, out matchIndex);
                if (matchTerm == null)
                {
                    MessageText.Inlines.Add(new Run(text.Substring(index)));
                    break;
                }

                if (matchIndex > index)
                    MessageText.Inlines.Add(new Run(text.Substring(index, matchIndex - index)));

                MessageText.Inlines.Add(new Run(text.Substring(matchIndex, matchTerm.Length))
                {
                    Background = HighlightBrush,
                    Foreground = Brushes.Black
                });
                index = matchIndex + matchTerm.Length;
            }
        }

        private string FindNextHighlight(string text, int startIndex, out int matchIndex)
        {
            matchIndex = -1;
            string matchTerm = null;

            foreach (string term in highlightTerms)
            {
                int index = text.IndexOf(term, startIndex, StringComparison.Ordinal);
                if (index < 0)
                    continue;

                if (matchIndex < 0 ||
                    index < matchIndex ||
                    (index == matchIndex && term.Length > matchTerm.Length))
                {
                    matchIndex = index;
                    matchTerm = term;
                }
            }

            return matchTerm;
        }

        private bool HasHighlightTerms()
        {
            return highlightTerms != null && highlightTerms.Count > 0;
        }

        private IList<string> CreateHighlightTerms(string highlightText)
        {
            List<string> terms = new List<string>();
            AddHighlightTerm(terms, highlightText);

            if (!string.IsNullOrWhiteSpace(highlightText))
            {
                int parenthesisIndex = highlightText.IndexOf('(');
                if (parenthesisIndex > 0)
                    AddHighlightTerm(terms, highlightText.Substring(0, parenthesisIndex));

                int slashIndex = highlightText.IndexOf('/');
                if (slashIndex > 0)
                    AddHighlightTerm(terms, highlightText.Substring(0, slashIndex));

                foreach (string japaneseTerm in ExtractJapaneseTerms(highlightText))
                    AddHighlightTerm(terms, japaneseTerm);
            }

            terms.Sort((left, right) => right.Length.CompareTo(left.Length));
            return terms;
        }

        private IEnumerable<string> ExtractJapaneseTerms(string value)
        {
            List<string> terms = new List<string>();
            StringBuilder builder = new StringBuilder();
            bool insideParentheses = false;

            foreach (char character in value)
            {
                if (character == '(' || character == '（')
                {
                    FlushJapaneseTerm(terms, builder);
                    insideParentheses = true;
                    continue;
                }

                if (character == ')' || character == '）')
                {
                    FlushJapaneseTerm(terms, builder);
                    insideParentheses = false;
                    continue;
                }

                if (insideParentheses)
                    continue;

                if (IsJapaneseCharacter(character))
                {
                    builder.Append(character);
                }
                else
                {
                    FlushJapaneseTerm(terms, builder);
                }
            }

            FlushJapaneseTerm(terms, builder);
            return terms;
        }

        private void FlushJapaneseTerm(IList<string> terms, StringBuilder builder)
        {
            if (builder.Length >= 2)
                terms.Add(builder.ToString());

            builder.Clear();
        }

        private bool IsJapaneseCharacter(char character)
        {
            return (character >= '\u3040' && character <= '\u30ff') ||
                   (character >= '\u3400' && character <= '\u9fff') ||
                   character == '々' ||
                   character == 'ヶ';
        }

        private void AddHighlightTerm(IList<string> terms, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string term = value.Trim();
            if (term.Length == 0 || terms.Contains(term))
                return;

            terms.Add(term);
        }

        private bool TrySplitChoice(string line, out string label, out string value)
        {
            label = string.Empty;
            value = string.Empty;

            if (string.IsNullOrWhiteSpace(line) || line.Length < 3)
                return false;

            char first = line[0];
            if (first < 'A' || first > 'D' || line[1] != '.')
                return false;

            label = line.Substring(0, 2);
            value = line.Substring(2);
            return true;
        }

        private bool TrySplitLabel(string line, out string label, out string value)
        {
            label = string.Empty;
            value = string.Empty;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            string[] knownLabels =
            {
                "等级", "主要内容", "释义", "释义/提示", "说明", "接续",
                "例句", "例句释义", "例句1", "例句1释义", "例句2", "例句2释义",
                "题目", "答案", "问题", "选项", "语法", "语法接续", "语法说明",
                "例句读音", "归属", "假名", "词性", "相关语法", "语法例句", "语法例句释义"
            };

            foreach (string knownLabel in knownLabels)
            {
                string chinesePrefix = knownLabel + "：";
                string asciiPrefix = knownLabel + ":";
                if (line.StartsWith(chinesePrefix, StringComparison.Ordinal))
                {
                    label = chinesePrefix;
                    value = line.Substring(chinesePrefix.Length);
                    return true;
                }
                if (line.StartsWith(asciiPrefix, StringComparison.Ordinal))
                {
                    label = asciiPrefix;
                    value = line.Substring(asciiPrefix.Length);
                    return true;
                }
            }

            return false;
        }

        private void AddButtons(IList<Tuple<string, string>> buttons)
        {
            ButtonPanel.Children.Clear();
            if (buttons == null || buttons.Count == 0)
                return;

            foreach (Tuple<string, string> buttonInfo in buttons)
            {
                Button button = new Button
                {
                    Content = buttonInfo.Item1,
                    Tag = buttonInfo.Item2,
                    Width = 82,
                    MinHeight = 38,
                    Margin = new Thickness(3, 4, 3, 0),
                    Padding = new Thickness(8, 4, 8, 4)
                };
                button.Click += ActionButton_Click;
                ButtonPanel.Children.Add(button);
            }
        }

        public void Complete(string action)
        {
            actionSelected = true;
            ActionSelected?.Invoke(action);
            Close();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Complete(button == null ? string.Empty : button.Tag as string);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Complete(Services.Notifications.NotificationAction.Cancel);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Left + 8;
            Top = workArea.Bottom - ActualHeight - 16;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!actionSelected)
                ActionSelected?.Invoke(Services.Notifications.NotificationAction.Cancel);
        }
    }
}
