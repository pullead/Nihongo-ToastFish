using System.Collections.Generic;
using System.Text;
using System.Windows;
using ToastFish.Services.Study;

namespace ToastFish
{
    public partial class StudyCardDetailWindow : Window
    {
        private static StudyCardDetailWindow activeWindow;

        public StudyCardDetailWindow(StudyCard card)
        {
            InitializeComponent();
            activeWindow?.Close();
            activeWindow = this;
            Closed += (sender, args) =>
            {
                if (ReferenceEquals(activeWindow, this))
                    activeWindow = null;
            };
            LoadCard(card);
        }

        public static bool CloseActiveWindow()
        {
            if (activeWindow == null)
                return false;

            StudyCardDetailWindow window = activeWindow;
            activeWindow = null;
            window.Close();
            return true;
        }

        private void LoadCard(StudyCard card)
        {
            if (card == null)
                return;

            TitleText.Text = string.IsNullOrWhiteSpace(card.Title) ? "学习内容详情" : card.Title;
            LevelText.Text = string.IsNullOrWhiteSpace(card.JlptLevel) ? string.Empty : "等级：" + card.JlptLevel;
            PrimaryText.Text = card.PrimaryText ?? string.Empty;
            SecondaryText.Text = card.SecondaryText ?? string.Empty;
            PromptText.Text = card.PromptText ?? string.Empty;
            DetailText.Text = card.DetailText ?? string.Empty;
            ChoicesText.Text = FormatChoices(card.Choices);
            AnswerText.Text = card.CorrectAnswer ?? string.Empty;

            SetVisibility(PromptLabel, PromptText, PromptText.Text);
            SetVisibility(DetailLabel, DetailText, DetailText.Text);
            SetVisibility(ChoicesLabel, ChoicesText, ChoicesText.Text);
            SetVisibility(AnswerLabel, AnswerText, AnswerText.Text);
        }

        private string FormatChoices(IList<string> choices)
        {
            if (choices == null || choices.Count == 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < choices.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(choices[index]))
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append((char)('A' + index));
                builder.Append(". ");
                builder.Append(choices[index].Trim());
            }

            return builder.ToString();
        }

        private void SetVisibility(UIElement label, UIElement text, string value)
        {
            Visibility visibility = string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
            label.Visibility = visibility;
            text.Visibility = visibility;
        }
    }
}
