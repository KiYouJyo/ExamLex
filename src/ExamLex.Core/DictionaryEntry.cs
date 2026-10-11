namespace ExamLex.Core;

/// <summary>A source-neutral dictionary entry; the first release intentionally ships no entries.</summary>
public sealed record DictionaryEntry(
    string Word,
    string? Phonetic,
    string? Translation,
    string? Definition,
    string? Cefr,
    IReadOnlyList<string> Examples,
    IReadOnlyList<string> ExamTags);
