namespace ToastFish.Model.Content
{
    public class GrammarExample
    {
        public string contentId { get; set; }
        public string packId { get; set; }
        public string grammarId { get; set; }
        public string grammarPattern { get; set; }
        public string grammarMeaningCn { get; set; }
        public string grammarFormation { get; set; }
        public string grammarUsageNote { get; set; }
        public string jlptLevel { get; set; }
        public string sentenceJp { get; set; }
        public string sentenceKana { get; set; }
        public string sentenceFuriganaJson { get; set; }
        public string meaningCn { get; set; }
        public string questionType { get; set; }
        public string promptCn { get; set; }
        public string correctAnswer { get; set; }
        public string distractorsJson { get; set; }
        public string choiceMeaningsJson { get; set; }
    }
}
