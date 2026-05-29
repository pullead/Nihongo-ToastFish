using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using ToastFish.Model.Content;
using ToastFish.Services.Japanese;

namespace ToastFish.Services.Study
{
    public class StudyCardFactory
    {
        private readonly FuriganaFormatter furiganaFormatter;

        public StudyCardFactory()
            : this(new FuriganaFormatter())
        {
        }

        public StudyCardFactory(FuriganaFormatter furiganaFormatter)
        {
            this.furiganaFormatter = furiganaFormatter ?? new FuriganaFormatter();
        }

        public StudyCard FromVocabulary(VocabularyItem item)
        {
            if (item == null)
                return null;

            string primary = furiganaFormatter.ToInlineText(item.furiganaJson, item.headword);
            string secondary = JoinParts(item.reading, item.partOfSpeech, item.meaningCn);
            string example = furiganaFormatter.ToInlineText(item.exampleFuriganaJson, item.exampleJp);

            return new StudyCard
            {
                CardId = "vocabulary:" + item.contentId,
                ContentId = item.contentId,
                Kind = StudyCardKind.Vocabulary,
                JlptLevel = item.jlptLevel,
                Title = primary,
                PrimaryText = primary,
                SecondaryText = secondary,
                DetailText = JoinLines(example, item.exampleCn),
                CorrectAnswer = item.meaningCn,
                Choices = new List<string>()
            };
        }

        public StudyCard FromGrammarPoint(GrammarPoint item)
        {
            if (item == null)
                return null;

            string primary = furiganaFormatter.ToInlineText(item.furiganaJson, item.pattern);

            return new StudyCard
            {
                CardId = "grammar:" + item.contentId,
                ContentId = item.contentId,
                Kind = StudyCardKind.Grammar,
                JlptLevel = item.jlptLevel,
                Title = item.pattern,
                PrimaryText = primary,
                SecondaryText = item.meaningCn,
                DetailText = JoinLines(item.formation, item.usageNote),
                CorrectAnswer = item.meaningCn,
                Choices = new List<string>()
            };
        }

        public StudyCard FromGrammarExample(GrammarExample item)
        {
            if (item == null)
                return null;

            string primary = furiganaFormatter.ToInlineText(item.sentenceFuriganaJson, item.sentenceJp);
            List<string> choices = ParseChoices(item.distractorsJson);
            if (!string.IsNullOrWhiteSpace(item.correctAnswer) && !choices.Contains(item.correctAnswer))
            {
                choices.Insert(0, item.correctAnswer);
            }

            return new StudyCard
            {
                CardId = "example:" + item.contentId,
                ContentId = item.contentId,
                Kind = StudyCardKind.Example,
                JlptLevel = item.jlptLevel,
                Title = item.questionType,
                PrimaryText = primary,
                SecondaryText = string.IsNullOrWhiteSpace(item.grammarPattern)
                    ? "语法：" + item.grammarId
                    : "语法：" + item.grammarPattern,
                DetailText = JoinLines(item.sentenceKana, item.meaningCn),
                PromptText = item.promptCn,
                CorrectAnswer = item.correctAnswer,
                Choices = choices
            };
        }

        public StudyCard FromGojuon(GojuonItem item)
        {
            if (item == null)
                return null;

            return new StudyCard
            {
                CardId = "gojuon:" + item.contentId,
                ContentId = item.contentId,
                Kind = StudyCardKind.Gojuon,
                PrimaryText = JoinParts(item.hiragana, item.katakana),
                SecondaryText = item.romaji,
                DetailText = item.audioPath,
                CorrectAnswer = item.romaji,
                Choices = new List<string>()
            };
        }

        private List<string> ParseChoices(string choicesJson)
        {
            if (string.IsNullOrWhiteSpace(choicesJson))
                return new List<string>();

            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<string>));
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(choicesJson)))
                {
                    return (List<string>)serializer.ReadObject(stream);
                }
            }
            catch
            {
                return new List<string>();
            }
        }

        private string JoinParts(params string[] parts)
        {
            List<string> values = new List<string>();
            foreach (string part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                    values.Add(part.Trim());
            }

            return string.Join(" / ", values);
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
