using ExamLex.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ExamLex.Core.Tests;

/// <summary>Checks the actual production query service against schema-v1 tables.</summary>
public sealed class DictionaryStoreTests
{
    [Fact]
    public async Task SearchPreservesEverySensePosAndNestedLexicalSection()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ExamLexTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new DictionaryStore(directory);
            using (var conn = new SqliteConnection($"Data Source={store.DatabasePath}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE entries (id INTEGER PRIMARY KEY, headword TEXT,
                        normalized_headword TEXT, language TEXT, cefr_level TEXT,
                        homograph_number INTEGER DEFAULT 1);
                    CREATE TABLE pronunciations (entry_id INTEGER, id INTEGER PRIMARY KEY,
                        ipa TEXT, accent TEXT, part_of_speech TEXT);
                    CREATE TABLE inflections (entry_id INTEGER, id INTEGER PRIMARY KEY,
                        part_of_speech TEXT, form_type TEXT, form TEXT);
                    CREATE TABLE senses (id INTEGER PRIMARY KEY, entry_id INTEGER,
                        part_of_speech TEXT, sense_number INTEGER, definition_en TEXT);
                    CREATE TABLE sense_translations (id INTEGER PRIMARY KEY,
                        sense_id INTEGER, language TEXT, text TEXT);
                    CREATE TABLE semantic_relations (id INTEGER PRIMARY KEY,
                        sense_id INTEGER, relation_type TEXT, related_word TEXT);
                    CREATE TABLE expressions (id INTEGER PRIMARY KEY,
                        sense_id INTEGER, expression_type TEXT, text_en TEXT,
                        meaning_zh TEXT, meaning_ja TEXT, notes TEXT);
                    CREATE TABLE examples (id INTEGER PRIMARY KEY,
                        sense_id INTEGER, text_en TEXT);
                    CREATE TABLE example_translations (id INTEGER PRIMARY KEY,
                        example_id INTEGER, language TEXT, text TEXT);
                    CREATE TABLE exam_tags (entry_id INTEGER, exam TEXT);
                    INSERT INTO entries(id, headword, normalized_headword, language, cefr_level)
                        VALUES (1, 'about', 'about', 'en', 'A1');
                    INSERT INTO pronunciations VALUES (1, 1, '/əˈbaʊt/', 'en-GB', 'prep');
                    INSERT INTO inflections VALUES (1, 1, 'prep', 'test', 'about');
                    INSERT INTO senses VALUES (1,1,'prep',1,'Concerning; regarding.');
                    INSERT INTO senses VALUES (2,1,'adv',1,'Approximately.');
                    INSERT INTO senses VALUES (3,1,'prep',2,'On the subject of.');
                    INSERT INTO senses VALUES (4,1,'prep',3,'Throughout.');
                    INSERT INTO senses VALUES (5,1,'prep',4,'Near.');
                    INSERT INTO senses VALUES (6,1,'prep',5,'Occupied with.');
                    INSERT INTO senses VALUES (7,1,'prep',6,'All around.');
                    INSERT INTO sense_translations VALUES (1,1,'zh','关于');
                    INSERT INTO sense_translations VALUES (2,1,'ja','～について');
                    INSERT INTO sense_translations VALUES (3,2,'zh','大约');
                    INSERT INTO semantic_relations VALUES (1,2,'synonym','approximately');
                    INSERT INTO expressions VALUES (1,1,'valency','talk about sth',
                        '谈论某事','何かについて話す',null);
                    INSERT INTO expressions VALUES (2,2,'collocation','about twenty',
                        '大约二十','約20',null);
                    INSERT INTO examples VALUES (10,2,'About twenty people came.');
                    INSERT INTO example_translations VALUES (1,10,'zh','大约二十人来了。');
                    INSERT INTO example_translations VALUES (2,10,'ja','約20人が来た。');
                    INSERT INTO exam_tags VALUES (1,'TOEFL');
                    """;
                cmd.ExecuteNonQuery();
            }

            var results = await store.SearchAsync("ABOUT");
            var word = Assert.Single(results);
            Assert.Equal("about", word.Word);
            Assert.Equal("A1", word.Cefr);
            Assert.Equal(7, word.Senses.Count); // NOT the former LIMIT 5.
            Assert.Equal(2, word.Senses.Select(x => x.PartOfSpeech).Distinct().Count());
            Assert.Equal("/əˈbaʊt/", Assert.Single(word.Pronunciations).Ipa);
            Assert.Single(word.Inflections);
            Assert.Equal("TOEFL", Assert.Single(word.ExamTags));

            var prep = Assert.Single(word.Senses, x => x.Id == 1);
            Assert.Equal("关于", Assert.Single(prep.Translations, x => x.Language == "zh").Text);
            Assert.Equal("～について", Assert.Single(prep.Translations, x => x.Language == "ja").Text);
            Assert.Equal("valency", Assert.Single(prep.Expressions).Type);
            Assert.Empty(prep.Examples);

            var adv = Assert.Single(word.Senses, x => x.Id == 2);
            Assert.Equal("大约", Assert.Single(adv.Translations).Text);
            Assert.Equal("approximately", Assert.Single(adv.Relations).RelatedWord);
            Assert.Equal("collocation", Assert.Single(adv.Expressions).Type);
            var example = Assert.Single(adv.Examples);
            Assert.Equal("About twenty people came.", example.English);
            Assert.Equal(2, example.Translations.Count);
            Assert.DoesNotContain(adv.Translations, x => x.Text == "关于");
            Assert.Empty(word.Senses.Single(x => x.Id == 3).Translations);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); }
            catch (IOException) { /* Windows SQLite handle cleanup lag; temp files can be cleared later. */ }
        }
    }
}
