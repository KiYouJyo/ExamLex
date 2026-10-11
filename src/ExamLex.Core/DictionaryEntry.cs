namespace ExamLex.Core;

/// <summary>
/// ExamLex display contract: preserve the source word, part of speech and
/// sense-level relationships. Missing content stays an empty collection/null.
/// The UI reserves every field independently of current dictionary coverage.
/// </summary>
public sealed record DictionaryEntry(
    string Word,
    string? Cefr,
    IReadOnlyList<DictionaryPronunciation> Pronunciations,
    IReadOnlyList<DictionaryInflection> Inflections,
    IReadOnlyList<DictionarySense> Senses,
    IReadOnlyList<string> ExamTags
);

public sealed record DictionaryPronunciation(
    string Ipa, string? Accent, string? PartOfSpeech
);
public sealed record DictionaryInflection(
    string PartOfSpeech, string FormType, string Form
);
public sealed record DictionaryTranslation(string Language, string Text);
public sealed record DictionaryRelation(string Type, string RelatedWord);
public sealed record DictionaryExpression(
    string Type, string English, string? Chinese, string? Japanese, string? Notes
);
public sealed record DictionaryExample(
    string English, IReadOnlyList<DictionaryTranslation> Translations
);
public sealed record DictionarySense(
    long Id, string PartOfSpeech, int Number, string? EnglishDefinition,
    IReadOnlyList<DictionaryTranslation> Translations,
    IReadOnlyList<DictionaryRelation> Relations,
    IReadOnlyList<DictionaryExpression> Expressions,
    IReadOnlyList<DictionaryExample> Examples
);
