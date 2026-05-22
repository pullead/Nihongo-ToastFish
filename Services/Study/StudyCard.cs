using System.Collections.Generic;

namespace ToastFish.Services.Study
{
    public class StudyCard
    {
        public string CardId { get; set; }
        public string ContentId { get; set; }
        public StudyCardKind Kind { get; set; }
        public string JlptLevel { get; set; }
        public string Title { get; set; }
        public string PrimaryText { get; set; }
        public string SecondaryText { get; set; }
        public string DetailText { get; set; }
        public string PromptText { get; set; }
        public string CorrectAnswer { get; set; }
        public IList<string> Choices { get; set; }
    }
}
