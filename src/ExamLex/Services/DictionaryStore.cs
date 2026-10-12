using ExamLex.Core;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExamLex.Services;

/// <summary>
/// Reads the complete canonical ExamLex-Lexicon SQLite schema v1 without
/// collapsing distinct senses into newline-separated strings.
/// All absent fields return empty collections, never fabricated text.
/// </summary>
public sealed class DictionaryStore
{
    public string DataDirectory { get; }
    public string DatabasePath => Path.Combine(DataDirectory, "lexicon-current.db");
    public DictionaryStore(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExamLex");
        Directory.CreateDirectory(DataDirectory);
    }


    /// <summary>
    /// Prefer the newest bundled, signed-release lexicon on first install and on
    /// upgrades. Never replace a locally newer independently downloaded lexicon.
    /// </summary>
    public void EnsureBundledLexicon()
    {
        string dir=Path.Combine(AppContext.BaseDirectory,"Assets","Lexicon");
        string bundled=Path.Combine(dir,"examlex-lexicon.db");
        string manifest=Path.Combine(dir,"lexicon-manifest.json");
        if(!File.Exists(bundled)||!File.Exists(manifest))return; // development build without bundle
        using var doc=JsonDocument.Parse(File.ReadAllText(manifest));
        var meta=doc.RootElement;
        string version=meta.GetProperty("version").GetString()??throw new InvalidDataException("词库版本缺失");
        string checksum=meta.GetProperty("sha256").GetString()??throw new InvalidDataException("词库校验信息缺失");
        if(meta.GetProperty("schema_version").GetInt32()!=1)
            throw new InvalidDataException("内置词库 Schema 不受支持");
        string state=Path.Combine(DataDirectory,"lexicon-state.json");
        if(File.Exists(DatabasePath))
        {
            if(!File.Exists(state))return; // never overwrite an unknown user database
            string current;
            try
            {
                using var existing=JsonDocument.Parse(File.ReadAllText(state));
                current=existing.RootElement.GetProperty("version").GetString()??"";
            }
            catch(JsonException){return;}
            if(!IsNewer(version,current))return;
        }
        if(!Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(bundled)))
            .Equals(checksum,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("内置词库 SHA256 校验失败");
        ValidateCanonicalSchema(bundled);
        string staging=Path.Combine(DataDirectory,"bundled-"+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            File.Copy(bundled,staging);
            File.Move(staging,DatabasePath,true);
            File.WriteAllText(state,JsonSerializer.Serialize(new {
                version,sha256=checksum,updated_at=DateTimeOffset.UtcNow,source="bundled-msix"
            }));
        }
        finally {if(File.Exists(staging))File.Delete(staging);}
    }

    private static bool IsNewer(string bundled,string installed)
    {
        static Version? ParseTag(string s)
        {
            var match=Regex.Match(s,@"(?:^|-)v(?<version>\d+\.\d+(?:\.\d+){0,2})$");
            return match.Success&&Version.TryParse(match.Groups["version"].Value,out var v)?v:null;
        }
        var a=ParseTag(bundled);
        var b=ParseTag(installed);
        return a is not null&&b is not null&&a>b;
    }

    public async Task<IReadOnlyList<DictionaryEntry>> SearchAsync(
        string prefix, CancellationToken ct = default)
    {
        if (!File.Exists(DatabasePath) || string.IsNullOrWhiteSpace(prefix)) return [];
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        await db.OpenAsync(ct);
        using var search = db.CreateCommand();
        search.CommandText = """
            SELECT id, headword, cefr_level
              FROM entries
             WHERE language='en' AND normalized_headword LIKE $query ESCAPE '~'
             ORDER BY CASE WHEN normalized_headword=$exact THEN 0 ELSE 1 END,
                      normalized_headword, homograph_number
             LIMIT 20
            """;
        var normalized = prefix.Trim().ToLowerInvariant();
        search.Parameters.AddWithValue("$exact", normalized);
        search.Parameters.AddWithValue("$query", normalized
            .Replace("~", "~~").Replace("%", "~%").Replace("_", "~_") + "%");

        var candidates = new List<(long Id, string Word, string? Cefr)>();
        using (var reader = await search.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                candidates.Add((reader.GetInt64(0), reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)));

        var results = new List<DictionaryEntry>(candidates.Count);
        foreach (var (id, word, cefr) in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var pronunciations = await ReadPronunciationsAsync(db, id, ct);
            var inflections = await ReadInflectionsAsync(db, id, ct);
            var senses = await ReadSensesAsync(db, id, ct);
            var tags = await ReadTagsAsync(db, id, ct);
            results.Add(new DictionaryEntry(
                word, cefr, pronunciations, inflections, senses, tags));
        }
        return results;
    }

    private static SqliteCommand Cmd(SqliteConnection db, string query, long id)
    {
        var cmd = db.CreateCommand();
        cmd.CommandText = query;
        cmd.Parameters.AddWithValue("$id", id);
        return cmd;
    }

    private static string? NullableText(SqliteDataReader reader, int index) =>
        reader.IsDBNull(index) ? null : reader.GetString(index);

    private static async Task<List<DictionaryPronunciation>> ReadPronunciationsAsync(
        SqliteConnection db, long id, CancellationToken ct)
    {
        using var cmd = Cmd(db,
            "SELECT ipa, accent, part_of_speech FROM pronunciations " +
            "WHERE entry_id=$id ORDER BY id", id);
        var items = new List<DictionaryPronunciation>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(new(reader.GetString(0), NullableText(reader, 1),
                NullableText(reader, 2)));
        return items;
    }

    private static async Task<List<DictionaryInflection>> ReadInflectionsAsync(
        SqliteConnection db, long id, CancellationToken ct)
    {
        using var cmd = Cmd(db,
            "SELECT part_of_speech, form_type, form FROM inflections " +
            "WHERE entry_id=$id ORDER BY part_of_speech, id", id);
        var items = new List<DictionaryInflection>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(new(reader.GetString(0), reader.GetString(1),
                reader.GetString(2)));
        return items;
    }

    private static async Task<List<DictionarySense>> ReadSensesAsync(
        SqliteConnection db, long entryId, CancellationToken ct)
    {
        using var command = Cmd(db,
            "SELECT id, part_of_speech, sense_number, definition_en FROM senses " +
            "WHERE entry_id=$id ORDER BY " +
            "CASE part_of_speech WHEN 'noun' THEN 1 WHEN 'verb' THEN 2 " +
            "WHEN 'adj' THEN 3 WHEN 'adv' THEN 4 WHEN 'prep' THEN 5 " +
            "WHEN 'pron' THEN 6 ELSE 7 END, part_of_speech, sense_number", entryId);
        var rows = new List<(long Id, string Pos, int Number, string? Definition)>();
        using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                rows.Add((reader.GetInt64(0), reader.GetString(1),
                    reader.GetInt32(2), NullableText(reader, 3)));

        // All related data is queried by sense ID, not by the headword.
        // It is never placed in a global bag of translations or examples.
        var translations = await ReadTranslationsAsync(db, entryId, ct);
        var relations = await ReadRelationsAsync(db, entryId, ct);
        var expressions = await ReadExpressionsAsync(db, entryId, ct);
        var examples = await ReadExamplesAsync(db, entryId, ct);

        return rows.Select(row => new DictionarySense(
            row.Id, row.Pos, row.Number, row.Definition,
            translations.GetValueOrDefault(row.Id) ?? [],
            relations.GetValueOrDefault(row.Id) ?? [],
            expressions.GetValueOrDefault(row.Id) ?? [],
            examples.GetValueOrDefault(row.Id) ?? []
        )).ToList();
    }

    private static async Task<Dictionary<long, List<DictionaryTranslation>>>
        ReadTranslationsAsync(SqliteConnection db, long id, CancellationToken ct)
    {
        using var cmd = Cmd(db, """
            SELECT t.sense_id, t.language, t.text
              FROM sense_translations t JOIN senses s ON s.id=t.sense_id
             WHERE s.entry_id=$id ORDER BY t.id
            """, id);
        var groups = new Dictionary<long, List<DictionaryTranslation>>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            long senseId = reader.GetInt64(0);
            if (!groups.TryGetValue(senseId, out var list))
                groups[senseId] = list = [];
            list.Add(new(reader.GetString(1), reader.GetString(2)));
        }
        return groups;
    }

    private static async Task<Dictionary<long, List<DictionaryRelation>>>
        ReadRelationsAsync(SqliteConnection db, long id, CancellationToken ct)
    {
        using var cmd = Cmd(db, """
            SELECT r.sense_id, r.relation_type, r.related_word
              FROM semantic_relations r JOIN senses s ON s.id=r.sense_id
             WHERE s.entry_id=$id ORDER BY r.id
            """, id);
        var groups = new Dictionary<long, List<DictionaryRelation>>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            long senseId = reader.GetInt64(0);
            if (!groups.TryGetValue(senseId, out var list))
                groups[senseId] = list = [];
            list.Add(new(reader.GetString(1), reader.GetString(2)));
        }
        return groups;
    }

    private static async Task<Dictionary<long, List<DictionaryExpression>>>
        ReadExpressionsAsync(SqliteConnection db, long id, CancellationToken ct)
    {
        using var cmd = Cmd(db, """
            SELECT e.sense_id, e.expression_type, e.text_en,
                   e.meaning_zh, e.meaning_ja, e.notes
              FROM expressions e JOIN senses s ON s.id=e.sense_id
             WHERE s.entry_id=$id ORDER BY e.id
            """, id);
        var groups = new Dictionary<long, List<DictionaryExpression>>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            long senseId = reader.GetInt64(0);
            if (!groups.TryGetValue(senseId, out var list))
                groups[senseId] = list = [];
            list.Add(new(reader.GetString(1), reader.GetString(2),
                NullableText(reader, 3), NullableText(reader, 4),
                NullableText(reader, 5)));
        }
        return groups;
    }

    private static async Task<Dictionary<long, List<DictionaryExample>>>
        ReadExamplesAsync(SqliteConnection db, long id, CancellationToken ct)
    {
        using var cmd = Cmd(db, """
            SELECT x.id, x.sense_id, x.text_en,
                   t.language, t.text
              FROM examples x
              JOIN senses s ON s.id=x.sense_id
              LEFT JOIN example_translations t ON t.example_id=x.id
             WHERE s.entry_id=$id ORDER BY x.id, t.id
            """, id);
        var texts = new Dictionary<long, (long SenseId, string Text)>();
        var translations = new Dictionary<long, List<DictionaryTranslation>>();
        using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                long exampleId = reader.GetInt64(0);
                texts.TryAdd(exampleId, (reader.GetInt64(1), reader.GetString(2)));
                if (!reader.IsDBNull(3) && !reader.IsDBNull(4))
                {
                    if (!translations.TryGetValue(exampleId, out var list))
                        translations[exampleId] = list = [];
                    list.Add(new(reader.GetString(3), reader.GetString(4)));
                }
            }
        }
        var result = new Dictionary<long, List<DictionaryExample>>();
        foreach (var (exampleId, item) in texts)
        {
            if (!result.TryGetValue(item.SenseId, out var list))
                result[item.SenseId] = list = [];
            list.Add(new(item.Text, translations.GetValueOrDefault(exampleId) ?? []));
        }
        return result;
    }

    private static async Task<List<string>> ReadTagsAsync(
        SqliteConnection db, long id, CancellationToken ct)
    {
        using var cmd = Cmd(db,
            "SELECT exam FROM exam_tags WHERE entry_id=$id ORDER BY exam", id);
        var items = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(reader.GetString(0));
        return items;
    }

    public static void ValidateCanonicalSchema(string path)
    {
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check";
        if ((string?)cmd.ExecuteScalar() != "ok")
            throw new InvalidDataException("SQLite 完整性验证失败");
        cmd.CommandText = "PRAGMA user_version";
        if (Convert.ToInt64(cmd.ExecuteScalar()) != 1)
            throw new InvalidDataException("词库 Schema 版本不兼容");

        foreach (var table in new[] {
            "entries", "senses", "pronunciations", "inflections",
            "sense_translations", "semantic_relations", "expressions",
            "examples", "example_translations", "exam_tags"
        })
        {
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master " +
                              "WHERE type='table' AND name=$name";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("$name", table);
            if (Convert.ToInt64(cmd.ExecuteScalar()) != 1)
                throw new InvalidDataException("缺少词库表：" + table);
        }
    }
}
