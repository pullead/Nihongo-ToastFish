namespace ToastFish.Model.Content
{
    public class VocabularyItem
    {
        public string contentId { get; set; }
        public string packId { get; set; }
        public string jlptLevel { get; set; }
        public string headword { get; set; }
        public string reading { get; set; }
        public string furiganaJson { get; set; }
        public string meaningCn { get; set; }
        public string partOfSpeech { get; set; }
        public string exampleJp { get; set; }
        public string exampleKana { get; set; }
        public string exampleFuriganaJson { get; set; }
        public string exampleCn { get; set; }
        public string sourceTags { get; set; }
        public string relatedGrammarPattern { get; set; }
        public string relatedGrammarMeaningCn { get; set; }
        public string relatedGrammarExampleJp { get; set; }
        public string relatedGrammarExampleCn { get; set; }
    }
}
