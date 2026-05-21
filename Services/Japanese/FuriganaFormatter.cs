using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using ToastFish.Model.Japanese;

namespace ToastFish.Services.Japanese
{
    public class FuriganaFormatter
    {
        public string ToInlineText(IEnumerable<FuriganaSegment> segments, bool showFurigana = true)
        {
            if (segments == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (FuriganaSegment segment in segments)
            {
                if (segment == null || string.IsNullOrEmpty(segment.Text))
                {
                    continue;
                }

                builder.Append(segment.Text);
                if (showFurigana && !string.IsNullOrEmpty(segment.Kana))
                {
                    builder.Append('(');
                    builder.Append(segment.Kana);
                    builder.Append(')');
                }
            }

            return builder.ToString();
        }

        public string ToInlineText(string furiganaJson, string fallbackText = "", bool showFurigana = true)
        {
            if (string.IsNullOrWhiteSpace(furiganaJson))
            {
                return fallbackText ?? string.Empty;
            }

            try
            {
                return ToInlineText(ParseJson(furiganaJson), showFurigana);
            }
            catch
            {
                return fallbackText ?? string.Empty;
            }
        }

        public IList<FuriganaSegment> ParseJson(string furiganaJson)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<FuriganaSegment>));
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(furiganaJson)))
            {
                return (IList<FuriganaSegment>)serializer.ReadObject(stream);
            }
        }
    }
}
