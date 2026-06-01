namespace ToastFish.Services.Notebook
{
    public class NotebookItem
    {
        public long id { get; set; }
        public string contentKind { get; set; }
        public string jlptLevel { get; set; }
        public string contentId { get; set; }
        public string title { get; set; }
        public string primaryText { get; set; }
        public string secondaryText { get; set; }
        public string detailText { get; set; }
        public string promptText { get; set; }
        public string correctAnswer { get; set; }
        public string createdAt { get; set; }
        public string highlightColor { get; set; }

        public string KindLabel
        {
            get
            {
                if (contentKind == "Vocabulary")
                    return "词汇";
                if (contentKind == "Grammar")
                    return "语法";
                if (contentKind == "Example")
                    return "例句";
                return contentKind;
            }
        }
    }
}
