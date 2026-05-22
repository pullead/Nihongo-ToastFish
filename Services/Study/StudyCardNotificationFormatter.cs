using System.Collections.Generic;
using System.Text;

namespace ToastFish.Services.Study
{
    public class StudyCardNotificationFormatter
    {
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
                    return JoinLines(card.PromptText, FormatChoices(card.Choices), card.PrimaryText);
                case StudyCardKind.Gojuon:
                    return JoinLines(card.PrimaryText, card.SecondaryText);
                default:
                    return JoinLines(card.PrimaryText, card.SecondaryText, card.DetailText);
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
    }
}
