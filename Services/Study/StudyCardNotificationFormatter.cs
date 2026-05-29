using System.Collections.Generic;
using System.Text;

namespace ToastFish.Services.Study
{
    public class StudyCardNotificationFormatter
    {
        private const int GrammarDetailSummaryLength = 86;
        private const int ExampleTextSummaryLength = 72;
        private const int VocabularyDetailSummaryLength = 96;

        public string Format(StudyCard card)
        {
            if (card == null)
                return string.Empty;

            switch (card.Kind)
            {
                case StudyCardKind.Vocabulary:
                    return JoinLines(card.PrimaryText, card.SecondaryText, card.DetailText);
                case StudyCardKind.Grammar:
                    return JoinLines(card.PrimaryText, card.SecondaryText, card.DetailText);
                case StudyCardKind.Example:
                    return JoinLines(card.SecondaryText, card.PromptText, FormatChoices(card.Choices), card.PrimaryText);
                case StudyCardKind.Gojuon:
                    return JoinLines(card.PrimaryText, card.SecondaryText);
                default:
                    return JoinLines(card.PrimaryText, card.SecondaryText, card.DetailText);
            }
        }

        public string FormatSummary(StudyCard card)
        {
            if (card == null)
                return string.Empty;

            switch (card.Kind)
            {
                case StudyCardKind.Vocabulary:
                    return JoinLines(
                        card.PrimaryText,
                        card.SecondaryText,
                        Summarize(card.DetailText, VocabularyDetailSummaryLength));
                case StudyCardKind.Grammar:
                    return JoinLines(
                        card.PrimaryText,
                        card.SecondaryText,
                        Summarize(card.DetailText, GrammarDetailSummaryLength));
                case StudyCardKind.Example:
                    return JoinLines(
                        card.SecondaryText,
                        Summarize(card.PromptText, ExampleTextSummaryLength),
                        Summarize(FormatChoices(card.Choices), ExampleTextSummaryLength),
                        Summarize(card.PrimaryText, ExampleTextSummaryLength));
                case StudyCardKind.Gojuon:
                    return JoinLines(card.PrimaryText, card.SecondaryText);
                default:
                    return JoinLines(
                        card.PrimaryText,
                        card.SecondaryText,
                        Summarize(card.DetailText, VocabularyDetailSummaryLength));
            }
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
                    builder.Append('\n');

                builder.Append((char)('A' + index));
                builder.Append(". ");
                builder.Append(choices[index].Trim());
            }

            return builder.ToString();
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

        private string Summarize(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Trim();
            if (normalized.Length <= maxLength)
                return normalized;

            return normalized.Substring(0, maxLength).TrimEnd() + "… 点击“详情”查看完整内容";
        }
    }
}
