using ExamLex.Core;
using Microsoft.Data.Sqlite;

namespace ExamLex.Services;

/// <summary>Reads ExamLex-Lexicon canonical SQLite schema v1.</summary>
public sealed class DictionaryStore
{
    public string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExamLex");
    public string DatabasePath => Path.Combine(DataDirectory, "lexicon-current.db");
    public DictionaryStore() => Directory.CreateDirectory(DataDirectory);

    public async Task<IReadOnlyList<DictionaryEntry>> SearchAsync(string prefix, CancellationToken ct = default)
    {
        if (!File.Exists(DatabasePath) || string.IsNullOrWhiteSpace(prefix)) return [];
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource=DatabasePath, Mode=SqliteOpenMode.ReadOnly, Pooling=false
        }.ToString());
        await db.OpenAsync(ct);
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
          SELECT id, headword, cefr_level,
             (SELECT ipa FROM pronunciations WHERE entry_id=entries.id ORDER BY id LIMIT 1)
          FROM entries
          WHERE language='en' AND normalized_headword LIKE $query ESCAPE '~'
          ORDER BY normalized_headword, homograph_number LIMIT 25
          """;
        cmd.Parameters.AddWithValue("$query", prefix.Trim().ToLowerInvariant()
            .Replace("~","~~").Replace("%","~%").Replace("_","~_") + "%");
        var words = new List<(long Id,string Word,string? Cefr,string? Ipa)>();
        using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                words.Add((reader.GetInt64(0), reader.GetString(1),
                    reader.IsDBNull(2)?null:reader.GetString(2),
                    reader.IsDBNull(3)?null:reader.GetString(3)));

        var results = new List<DictionaryEntry>();
        foreach (var word in words)
        {
            var defs = await ChildAsync(db, """
                SELECT s.part_of_speech || '. ' || s.definition_en FROM senses s
                WHERE s.entry_id=$id AND s.definition_en IS NOT NULL ORDER BY s.id LIMIT 5
                """,word.Id,ct);
            var zh = await ChildAsync(db, """
                SELECT t.text FROM sense_translations t JOIN senses s ON s.id=t.sense_id
                WHERE s.entry_id=$id AND t.language='zh' ORDER BY s.id LIMIT 6
                """,word.Id,ct);
            var ex = await ChildAsync(db, """
                SELECT x.text_en FROM examples x JOIN senses s ON s.id=x.sense_id
                WHERE s.entry_id=$id ORDER BY x.id LIMIT 3
                """,word.Id,ct);
            var tags = await ChildAsync(db,"SELECT exam FROM exam_tags WHERE entry_id=$id ORDER BY exam",word.Id,ct);
            results.Add(new DictionaryEntry(word.Word,word.Ipa,
                zh.Count>0?string.Join("；",zh):null,
                defs.Count>0?string.Join("\n",defs):null,
                word.Cefr,ex,tags));
        }
        return results;
    }
    private static async Task<List<string>> ChildAsync(SqliteConnection db,string sql,long id,CancellationToken ct)
    {
        using var c=db.CreateCommand();c.CommandText=sql;c.Parameters.AddWithValue("$id",id);
        using var r=await c.ExecuteReaderAsync(ct);
        var items=new List<string>();
        while(await r.ReadAsync(ct)) if(!r.IsDBNull(0)) items.Add(r.GetString(0));
        return items;
    }
    public static void ValidateCanonicalSchema(string path)
    {
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource=path,Mode=SqliteOpenMode.ReadOnly,Pooling=false }.ToString());
        db.Open();
        using var cmd=db.CreateCommand();
        cmd.CommandText="PRAGMA integrity_check";
        if((string?)cmd.ExecuteScalar()!="ok") throw new InvalidDataException("SQLite 完整性验证失败");
        cmd.CommandText="PRAGMA user_version";
        if(Convert.ToInt64(cmd.ExecuteScalar())!=1) throw new InvalidDataException("词库 Schema 版本不兼容");
        foreach(var table in new[]{"entries","senses","pronunciations","examples","sense_translations","exam_tags"})
        {
            cmd.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
            cmd.Parameters.Clear();cmd.Parameters.AddWithValue("$name",table);
            if(Convert.ToInt64(cmd.ExecuteScalar())!=1)
                throw new InvalidDataException("缺少词库表："+table);
        }
    }
}
