using System;

namespace ToastFish.Model.Study
{
    public class ReviewCard
    {
        public string reviewCardId { get; set; }
        public string contentId { get; set; }
        public string contentKind { get; set; }
        public string status { get; set; }
        public DateTime? dueAt { get; set; }
        public DateTime? lastReviewedAt { get; set; }
        public int reviewCount { get; set; }
        public double easeFactor { get; set; }
        public double intervalDays { get; set; }
        public int lapses { get; set; }
    }
}
