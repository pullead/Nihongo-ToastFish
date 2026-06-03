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
            string example = furiganaFormatter.ToInlineText(item.exampleFuriganaJson, item.exampleJp);

            return new StudyCard
            {
                CardId = "vocabulary:" + item.contentId,
                ContentId = item.contentId,
                Kind = StudyCardKind.Vocabulary,
                JlptLevel = item.jlptLevel,
                Title = primary,
                PrimaryText = primary,
                SecondaryText = Label("归属", item.sourceTags),
                DetailText = JoinLines(
                    Label("假名", item.reading),
                    Label("词性", string.IsNullOrWhiteSpace(item.partOfSpeech) ? "未标注" : item.partOfSpeech),
                    Label("释义", item.meaningCn),
                    Label("例句", example),
                    Label("例句释义", item.exampleCn),
                    Label("相关语法", JoinParts(item.relatedGrammarPattern, item.relatedGrammarMeaningCn)),
                    Label("语法例句", item.relatedGrammarExampleJp),
                    Label("语法例句释义", item.relatedGrammarExampleCn)),
                CorrectAnswer = item.meaningCn,
                Choices = new List<string>(),
                ChoiceMeanings = new Dictionary<string, string>()
            };
        }

        public StudyCard FromGrammarPoint(GrammarPoint item)
        {
            if (item == null)
                return null;

            string primary = furiganaFormatter.ToInlineText(item.furiganaJson, item.pattern);
            string example = furiganaFormatter.ToInlineText(item.exampleFuriganaJson, item.exampleSentenceJp);
            string secondExample = furiganaFormatter.ToInlineText(item.secondExampleFuriganaJson, item.secondExampleSentenceJp);

            return new StudyCard
            {
                CardId = "grammar:" + item.contentId,
                ContentId = item.contentId,
                Kind = StudyCardKind.Grammar,
                JlptLevel = item.jlptLevel,
                Title = item.pattern,
                PrimaryText = primary,
                SecondaryText = item.meaningCn,
                DetailText = JoinLines(
                    Label("接续", item.formation),
                    Label("说明", item.usageNote),
                    Label("例句1", example),
                    Label("例句1释义", item.exampleMeaningCn),
                    Label("例句2", secondExample),
                    Label("例句2释义", item.secondExampleMeaningCn)),
                CorrectAnswer = item.meaningCn,
                Choices = new List<string>(),
                ChoiceMeanings = new Dictionary<string, string>()
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
                SecondaryText = "语法：" + JoinParts(
                    string.IsNullOrWhiteSpace(item.grammarPattern) ? item.grammarId : item.grammarPattern,
                    item.grammarMeaningCn),
                DetailText = JoinLines(
                    Label("题目", item.promptCn),
                    Label("例句读音", item.sentenceKana),
                    Label("例句释义", item.meaningCn),
                    Label("语法接续", item.grammarFormation),
                    Label("语法说明", item.grammarUsageNote)),
                PromptText = item.promptCn,
                CorrectAnswer = item.correctAnswer,
                Choices = choices,
                ChoiceMeanings = ParseChoiceMeanings(item.choiceMeaningsJson)
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
                Choices = new List<string>(),
                ChoiceMeanings = new Dictionary<string, string>()
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

        private Dictionary<string, string> ParseChoiceMeanings(string meaningsJson)
        {
            if (string.IsNullOrWhiteSpace(meaningsJson))
                return new Dictionary<string, string>();

            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>));
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(meaningsJson)))
                {
                    return (Dictionary<string, string>)serializer.ReadObject(stream);
                }
            }
            catch
            {
                return new Dictionary<string, string>();
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

        private string Label(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return label + "：" + value.Trim();
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
