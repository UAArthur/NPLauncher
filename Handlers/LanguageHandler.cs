namespace BPLauncher.Handlers;

public class LanguageHandler
{
    private struct Language
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    private Language _selectedLanguage;
}