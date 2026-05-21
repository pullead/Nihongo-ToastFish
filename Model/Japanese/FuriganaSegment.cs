using System.Runtime.Serialization;

namespace ToastFish.Model.Japanese
{
    [DataContract]
    public class FuriganaSegment
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "kana", EmitDefaultValue = false)]
        public string Kana { get; set; }
    }
}
