namespace VNTextPatch.Shared
{
    public enum ScriptStringType
    {
        CharacterName,
        Message,
        Internal
    }

    internal enum ExcelColumn
    {
        OriginalCharacter = 1,
        OriginalLine = 2,
        TranslatedCharacter = 3,
        TranslatedLine = 4,
        CheckedLine = 5,
        EditedLine = 6,
        Notes = 7
    }
}
