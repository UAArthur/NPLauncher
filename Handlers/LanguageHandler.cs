using System.Reflection;
using System.Resources;
using BPLauncher.utils;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BPLauncher.Handlers
{
    public class LanguageHandler
    {
        private struct Language
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string JsonFile { get; set; }
            public string PakFile { get; set; }
            public string SigFile { get; set; }
        }

        private readonly List<Language> _languages = [];

        public LanguageHandler()
        {
            _ = LoadLanguagesAsync();
        }

        private async Task LoadLanguagesAsync()
        {
            await LoadLanguages();
        }

        private async Task LoadLanguages()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetName().Name + ".g.resources";
            Logger.Debug("Loading languages from embedded resources...");

            try
            {
                await using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return;
                using var reader = new ResourceReader(stream);
                foreach (System.Collections.DictionaryEntry entry in reader)
                {
                    var resourceKey = (string)entry.Key;
                    if (!resourceKey.StartsWith("assets/lang")) continue;
                    var langCode = resourceKey.Split('/')[2];
                    var language = new Language
                    {
                        Name = langCode,
                        Code = langCode,
                        JsonFile = $"pack://application:,,,/{assembly.GetName().Name};component/{resourceKey}.json",
                        PakFile = $"pack://application:,,,/{assembly.GetName().Name};component/{resourceKey}.pak",
                        SigFile = $"pack://application:,,,/{assembly.GetName().Name};component/{resourceKey}.sig"
                    };

                    _languages.Add(language);
                    Logger.Debug($"Loaded language: {langCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading languages: " + ex.Message);
            }
        }
    }
}