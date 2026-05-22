using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace ToastFish.Services.ContentUpdate
{
    public class ContentPackImporter
    {
        public ContentImportResult ImportManifest(string manifestPath, SQLiteConnection database)
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
                throw new ArgumentException("Manifest path is required.", nameof(manifestPath));
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            string fullManifestPath = Path.GetFullPath(manifestPath);
            string manifestDirectory = Path.GetDirectoryName(fullManifestPath);
            ContentManifest manifest = ReadJson<ContentManifest>(fullManifestPath);
            if (manifest.Packs == null || manifest.Packs.Count == 0)
                throw new InvalidDataException("Manifest does not contain any packs.");

            List<VerifiedPack> verifiedPacks = new List<VerifiedPack>();
            foreach (PackReference packReference in manifest.Packs)
            {
                ValidatePackReference(packReference);
                string packPath = ResolvePackPath(manifestDirectory, manifest.BaseUrl, packReference.Path);
                VerifyHash(packPath, packReference.Sha256);
                verifiedPacks.Add(new VerifiedPack(packReference, ReadJson<ContentPackFile>(packPath)));
            }

            ContentImportResult result = new ContentImportResult();
            HashSet<string> importedSourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SQLiteTransaction transaction = database.BeginTransaction())
            {
                foreach (VerifiedPack verifiedPack in verifiedPacks)
                {
                    ImportPack(verifiedPack, database, transaction, result, importedSourceIds);
                }

                transaction.Commit();
            }

            return result;
        }

        private void ImportPack(
            VerifiedPack verifiedPack,
            SQLiteConnection database,
            SQLiteTransaction transaction,
            ContentImportResult result,
            HashSet<string> importedSourceIds)
        {
            PackReference packReference = verifiedPack.Reference;
            ContentPackFile packFile = verifiedPack.PackFile;
            PackMetadata pack = packFile.Pack;

            if (pack == null)
                throw new InvalidDataException(packReference.Path + " does not contain pack metadata.");
            if (pack.PackId != packReference.PackId)
                throw new InvalidDataException(packReference.Path + " packId does not match manifest.");
            if (pack.ContentKind != packReference.ContentKind)
                throw new InvalidDataException(packReference.Path + " contentKind does not match manifest.");
            if (packFile.Items == null || packFile.Items.Count == 0)
                throw new InvalidDataException(packReference.Path + " does not contain any items.");

            UpsertSource(pack.Source, pack.License, database, transaction);
            if (importedSourceIds.Add(pack.Source.SourceId))
                result.SourcesImported++;

            UpsertContentPack(packReference, pack, database, transaction);
            result.PacksImported++;

            DeleteExistingItems(pack.PackId, pack.ContentKind, database, transaction);

            foreach (ContentItem item in packFile.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ContentId))
                    throw new InvalidDataException(packReference.Path + " contains an item without contentId.");

                switch (pack.ContentKind)
                {
                    case "gojuon":
                        UpsertGojuonItem(pack.PackId, item, database, transaction);
                        result.GojuonItemsImported++;
                        break;
                    case "vocabulary":
                        UpsertVocabularyItem(pack.PackId, pack.JlptLevel, item, database, transaction);
                        result.VocabularyItemsImported++;
                        break;
                    case "grammar":
                        UpsertGrammarPoint(pack.PackId, pack.JlptLevel, item, database, transaction);
                        result.GrammarPointsImported++;
                        break;
                    case "example":
                        UpsertGrammarExample(pack.PackId, pack.JlptLevel, item, database, transaction);
                        result.GrammarExamplesImported++;
                        break;
                    default:
                        throw new InvalidDataException("Unsupported content kind: " + pack.ContentKind);
                }
            }
        }

        private void UpsertSource(SourceMetadata source, LicenseMetadata license, SQLiteConnection database, SQLiteTransaction transaction)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.SourceId))
                throw new InvalidDataException("Content source is required.");

            ExecuteNonQuery(
                database,
                transaction,
                @"INSERT OR REPLACE INTO ContentSource
                    (sourceId, name, url, licenseName, licenseUrl, attribution, notes)
                  VALUES
                    (@sourceId, @name, @url, @licenseName, @licenseUrl, @attribution, @notes)",
                new Dictionary<string, object>
                {
                    { "@sourceId", source.SourceId },
                    { "@name", source.Name },
                    { "@url", source.Url },
                    { "@licenseName", license == null ? null : license.Name },
                    { "@licenseUrl", license == null ? null : license.Url },
                    { "@attribution", source.Attribution },
                    { "@notes", source.Notes }
                });
        }

        private void UpsertContentPack(PackReference packReference, PackMetadata pack, SQLiteConnection database, SQLiteTransaction transaction)
        {
            ExecuteNonQuery(
                database,
                transaction,
                @"INSERT OR REPLACE INTO ContentPack
                    (packId, version, jlptLevel, contentKind, displayName, description, sourceId, licenseName, licenseUrl, contentHash, installedAt)
                  VALUES
                    (@packId, @version, @jlptLevel, @contentKind, @displayName, @description, @sourceId, @licenseName, @licenseUrl, @contentHash, @installedAt)",
                new Dictionary<string, object>
                {
                    { "@packId", pack.PackId },
                    { "@version", pack.Version },
                    { "@jlptLevel", pack.JlptLevel },
                    { "@contentKind", pack.ContentKind },
                    { "@displayName", pack.DisplayName },
                    { "@description", pack.Description },
                    { "@sourceId", pack.Source == null ? null : pack.Source.SourceId },
                    { "@licenseName", pack.License == null ? null : pack.License.Name },
                    { "@licenseUrl", pack.License == null ? null : pack.License.Url },
                    { "@contentHash", packReference.Sha256 },
                    { "@installedAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) }
                });
        }

        private void DeleteExistingItems(string packId, string contentKind, SQLiteConnection database, SQLiteTransaction transaction)
        {
            string tableName;
            switch (contentKind)
            {
                case "gojuon":
                    tableName = "GojuonItem";
                    break;
                case "vocabulary":
                    tableName = "VocabularyItem";
                    break;
                case "grammar":
                    tableName = "GrammarPoint";
                    break;
                case "example":
                    tableName = "GrammarExample";
                    break;
                default:
                    throw new InvalidDataException("Unsupported content kind: " + contentKind);
            }

            ExecuteNonQuery(
                database,
                transaction,
                "DELETE FROM " + tableName + " WHERE packId = @packId",
                new Dictionary<string, object> { { "@packId", packId } });
        }

        private void UpsertGojuonItem(string packId, ContentItem item, SQLiteConnection database, SQLiteTransaction transaction)
        {
            ExecuteNonQuery(
                database,
                transaction,
                @"INSERT OR REPLACE INTO GojuonItem
                    (contentId, packId, romaji, hiragana, katakana, audioPath)
                  VALUES
                    (@contentId, @packId, @romaji, @hiragana, @katakana, @audioPath)",
                new Dictionary<string, object>
                {
                    { "@contentId", item.ContentId },
                    { "@packId", packId },
                    { "@romaji", item.Romaji },
                    { "@hiragana", item.Hiragana },
                    { "@katakana", item.Katakana },
                    { "@audioPath", item.AudioPath }
                });
        }

        private void UpsertVocabularyItem(string packId, string jlptLevel, ContentItem item, SQLiteConnection database, SQLiteTransaction transaction)
        {
            ExecuteNonQuery(
                database,
                transaction,
                @"INSERT OR REPLACE INTO VocabularyItem
                    (contentId, packId, jlptLevel, headword, reading, furiganaJson, meaningCn, partOfSpeech, exampleJp, exampleKana, exampleFuriganaJson, exampleCn)
                  VALUES
                    (@contentId, @packId, @jlptLevel, @headword, @reading, @furiganaJson, @meaningCn, @partOfSpeech, @exampleJp, @exampleKana, @exampleFuriganaJson, @exampleCn)",
                new Dictionary<string, object>
                {
                    { "@contentId", item.ContentId },
                    { "@packId", packId },
                    { "@jlptLevel", jlptLevel },
                    { "@headword", item.Headword },
                    { "@reading", item.Reading },
                    { "@furiganaJson", item.FuriganaJson },
                    { "@meaningCn", item.MeaningCn },
                    { "@partOfSpeech", item.PartOfSpeech },
                    { "@exampleJp", item.ExampleJp },
                    { "@exampleKana", item.ExampleKana },
                    { "@exampleFuriganaJson", item.ExampleFuriganaJson },
                    { "@exampleCn", item.ExampleCn }
                });
        }

        private void UpsertGrammarPoint(string packId, string jlptLevel, ContentItem item, SQLiteConnection database, SQLiteTransaction transaction)
        {
            ExecuteNonQuery(
                database,
                transaction,
                @"INSERT OR REPLACE INTO GrammarPoint
                    (contentId, packId, jlptLevel, pattern, meaningCn, formation, usageNote, furiganaJson)
                  VALUES
                    (@contentId, @packId, @jlptLevel, @pattern, @meaningCn, @formation, @usageNote, @furiganaJson)",
                new Dictionary<string, object>
                {
                    { "@contentId", item.ContentId },
                    { "@packId", packId },
                    { "@jlptLevel", jlptLevel },
                    { "@pattern", item.Pattern },
                    { "@meaningCn", item.MeaningCn },
                    { "@formation", item.Formation },
                    { "@usageNote", item.UsageNote },
                    { "@furiganaJson", item.FuriganaJson }
                });
        }

        private void UpsertGrammarExample(string packId, string jlptLevel, ContentItem item, SQLiteConnection database, SQLiteTransaction transaction)
        {
            ExecuteNonQuery(
                database,
                transaction,
                @"INSERT OR REPLACE INTO GrammarExample
                    (contentId, packId, grammarId, jlptLevel, sentenceJp, sentenceKana, sentenceFuriganaJson, meaningCn, questionType, promptCn, correctAnswer, distractorsJson)
                  VALUES
                    (@contentId, @packId, @grammarId, @jlptLevel, @sentenceJp, @sentenceKana, @sentenceFuriganaJson, @meaningCn, @questionType, @promptCn, @correctAnswer, @distractorsJson)",
                new Dictionary<string, object>
                {
                    { "@contentId", item.ContentId },
                    { "@packId", packId },
                    { "@grammarId", item.GrammarId },
                    { "@jlptLevel", jlptLevel },
                    { "@sentenceJp", item.SentenceJp },
                    { "@sentenceKana", item.SentenceKana },
                    { "@sentenceFuriganaJson", item.SentenceFuriganaJson },
                    { "@meaningCn", item.MeaningCn },
                    { "@questionType", item.QuestionType },
                    { "@promptCn", item.PromptCn },
                    { "@correctAnswer", item.CorrectAnswer },
                    { "@distractorsJson", SerializeStringList(item.Distractors) }
                });
        }

        private void ExecuteNonQuery(
            SQLiteConnection database,
            SQLiteTransaction transaction,
            string commandText,
            IDictionary<string, object> parameters)
        {
            using (SQLiteCommand command = database.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = commandText;
                foreach (KeyValuePair<string, object> parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }

                command.ExecuteNonQuery();
            }
        }

        private T ReadJson<T>(string path)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            using (FileStream stream = File.OpenRead(path))
            {
                return (T)serializer.ReadObject(stream);
            }
        }

        private string SerializeStringList(List<string> values)
        {
            if (values == null)
                return null;

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<string>));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, values);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private void ValidatePackReference(PackReference packReference)
        {
            if (packReference == null)
                throw new InvalidDataException("Manifest contains an empty pack reference.");
            if (string.IsNullOrWhiteSpace(packReference.PackId))
                throw new InvalidDataException("Pack reference is missing packId.");
            if (string.IsNullOrWhiteSpace(packReference.Path))
                throw new InvalidDataException(packReference.PackId + " is missing path.");
            if (string.IsNullOrWhiteSpace(packReference.Sha256))
                throw new InvalidDataException(packReference.PackId + " is missing sha256.");
        }

        private string ResolvePackPath(string manifestDirectory, string baseUrl, string packPath)
        {
            if (Path.IsPathRooted(packPath))
                throw new InvalidDataException("Pack path must be relative.");

            string root = Path.GetFullPath(manifestDirectory);
            string combined = Path.GetFullPath(Path.Combine(root, baseUrl ?? string.Empty, packPath));
            if (!combined.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Pack path escapes the manifest directory.");

            return combined;
        }

        private void VerifyHash(string path, string expectedSha256)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                string actual = ToHex(sha256.ComputeHash(stream));
                if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Hash verification failed for " + Path.GetFileName(path) + ".");
            }
        }

        private string ToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private class VerifiedPack
        {
            public VerifiedPack(PackReference reference, ContentPackFile packFile)
            {
                Reference = reference;
                PackFile = packFile;
            }

            public PackReference Reference { get; private set; }
            public ContentPackFile PackFile { get; private set; }
        }

        [DataContract]
        private class ContentManifest
        {
            [DataMember(Name = "baseUrl")]
            public string BaseUrl { get; set; }

            [DataMember(Name = "packs")]
            public List<PackReference> Packs { get; set; }
        }

        [DataContract]
        private class PackReference
        {
            [DataMember(Name = "packId")]
            public string PackId { get; set; }

            [DataMember(Name = "version")]
            public string Version { get; set; }

            [DataMember(Name = "jlptLevel")]
            public string JlptLevel { get; set; }

            [DataMember(Name = "contentKind")]
            public string ContentKind { get; set; }

            [DataMember(Name = "path")]
            public string Path { get; set; }

            [DataMember(Name = "sha256")]
            public string Sha256 { get; set; }
        }

        [DataContract]
        private class ContentPackFile
        {
            [DataMember(Name = "pack")]
            public PackMetadata Pack { get; set; }

            [DataMember(Name = "items")]
            public List<ContentItem> Items { get; set; }
        }

        [DataContract]
        private class PackMetadata
        {
            [DataMember(Name = "packId")]
            public string PackId { get; set; }

            [DataMember(Name = "version")]
            public string Version { get; set; }

            [DataMember(Name = "jlptLevel")]
            public string JlptLevel { get; set; }

            [DataMember(Name = "contentKind")]
            public string ContentKind { get; set; }

            [DataMember(Name = "displayName")]
            public string DisplayName { get; set; }

            [DataMember(Name = "description")]
            public string Description { get; set; }

            [DataMember(Name = "source")]
            public SourceMetadata Source { get; set; }

            [DataMember(Name = "license")]
            public LicenseMetadata License { get; set; }

        }

        [DataContract]
        private class SourceMetadata
        {
            [DataMember(Name = "sourceId")]
            public string SourceId { get; set; }

            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "url")]
            public string Url { get; set; }

            [DataMember(Name = "attribution")]
            public string Attribution { get; set; }

            [DataMember(Name = "notes")]
            public string Notes { get; set; }
        }

        [DataContract]
        private class LicenseMetadata
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "url")]
            public string Url { get; set; }
        }

        [DataContract]
        private class ContentItem
        {
            [DataMember(Name = "contentId")]
            public string ContentId { get; set; }

            [DataMember(Name = "romaji")]
            public string Romaji { get; set; }

            [DataMember(Name = "hiragana")]
            public string Hiragana { get; set; }

            [DataMember(Name = "katakana")]
            public string Katakana { get; set; }

            [DataMember(Name = "audioPath")]
            public string AudioPath { get; set; }

            [DataMember(Name = "headword")]
            public string Headword { get; set; }

            [DataMember(Name = "reading")]
            public string Reading { get; set; }

            [DataMember(Name = "furiganaJson")]
            public string FuriganaJson { get; set; }

            [DataMember(Name = "meaningCn")]
            public string MeaningCn { get; set; }

            [DataMember(Name = "partOfSpeech")]
            public string PartOfSpeech { get; set; }

            [DataMember(Name = "exampleJp")]
            public string ExampleJp { get; set; }

            [DataMember(Name = "exampleKana")]
            public string ExampleKana { get; set; }

            [DataMember(Name = "exampleFuriganaJson")]
            public string ExampleFuriganaJson { get; set; }

            [DataMember(Name = "exampleCn")]
            public string ExampleCn { get; set; }

            [DataMember(Name = "pattern")]
            public string Pattern { get; set; }

            [DataMember(Name = "formation")]
            public string Formation { get; set; }

            [DataMember(Name = "usageNote")]
            public string UsageNote { get; set; }

            [DataMember(Name = "grammarId")]
            public string GrammarId { get; set; }

            [DataMember(Name = "sentenceJp")]
            public string SentenceJp { get; set; }

            [DataMember(Name = "sentenceKana")]
            public string SentenceKana { get; set; }

            [DataMember(Name = "sentenceFuriganaJson")]
            public string SentenceFuriganaJson { get; set; }

            [DataMember(Name = "questionType")]
            public string QuestionType { get; set; }

            [DataMember(Name = "promptCn")]
            public string PromptCn { get; set; }

            [DataMember(Name = "correctAnswer")]
            public string CorrectAnswer { get; set; }

            [DataMember(Name = "distractors")]
            public List<string> Distractors { get; set; }
        }
    }
}
