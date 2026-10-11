using System.Text.Json;
using ExamLex.Core;
using Microsoft.Data.Sqlite;

namespace ExamLex.Services;

/// <summary>
/// A fresh user-local database; intentionally never copies or unpacks Flint/ECDICT word data.
/// Example sentences, CEFR, and exam tags are nullable/empty until licensed data is imported.
/// </summary>
public sealed class DictionaryStore
{
    public string DatabasePath { get; }

    public DictionaryStore()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExamLex");
        Directory.CreateDirectory(directory);
        DatabasePath = Path.Combine(directory, "lexicon.db");
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS entries (
                word TEXT PRIMARY KEY COLLATE NOCASE,
                phonetic TEXT,
                translation TEXT,
                definition TEXT,
                cefr TEXT,
                examples_json TEXT NOT NULL DEFAULT '[]',
                exam_tags_json TEXT NOT NULL DEFAULT '[]'
            );
            CREATE INDEX IF NOT EXISTS idx_entries_word ON entries(word);
            """;
        command.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<DictionaryEntry>> SearchAsync(
        string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return [];
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = """
            SELECT word, phonetic, translation, definition, cefr, examples_json, exam_tags_json
              FROM entries WHERE word LIKE $prefix ESCAPE '~'
              ORDER BY word COLLATE NOCASE LIMIT 25;
            """;
        command.Parameters.AddWithValue("$prefix",
            prefix.Trim().Replace("~", "~~").Replace("%", "~%").Replace("_", "~_") + "%");
        var entries = new List<DictionaryEntry>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new DictionaryEntry(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                ParseList(reader.GetString(5)),
                ParseList(reader.GetString(6))));
        }
        return entries;
    }

    private static IReadOnlyList<string> ParseList(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private SqliteConnection Open()
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        db.Open();
        return db;
    }
}
