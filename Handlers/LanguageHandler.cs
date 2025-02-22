using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Shapes;
using BPLauncher.Config;
using BPLauncher.services;
using BPLauncher.utils;
using Microsoft.VisualBasic.Logging;
using Path = System.IO.Path;

namespace BPLauncher.Handlers
{
    public class LanguageHandler
    {
        public struct Language
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string JsonFile { get; set; }
            public string PakFile { get; set; }
            public string SigFile { get; set; }
        }

        private static readonly List<Language> _languages = [];
        private static Language _currentLanguage;

        public static async Task LoadLanguagesAsync()
        {
            await LoadLanguages();
        }

        private static async Task LoadLanguages()
        {
            _languages.Clear();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetName().Name + ".g.resources";
            Logger.Debug("Loading languages from embedded resources...");

            try
            {
                await using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return;
                using var reader = new ResourceReader(stream);
                foreach (DictionaryEntry entry in reader)
                {
                    var resourceKey = (string)entry.Key;
                    if (!resourceKey.StartsWith("assets/lang")) continue;

                    var langCode = resourceKey.Split('/')[2];

                    if (_languages.Any(l => l.Code == langCode)) continue;

                    var language = new Language
                    {
                        Name = langCode,
                        Code = langCode,
                        JsonFile =
                            $"pack://application:,,,/{assembly.GetName().Name};component/assets/lang/{langCode}/{langCode}.json",
                        PakFile =
                            $"pack://application:,,,/{assembly.GetName().Name};component/assets/lang/{langCode}/blast_translation_mod_1_p.pak",
                        SigFile =
                            $"pack://application:,,,/{assembly.GetName().Name};component/assets/lang/{langCode}/blast_translation_mod_1_p.sig"
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

        public static async Task SwitchGameLanguage(string languageCode)
        {
            Language? language = _languages.FirstOrDefault(l => l.Code == languageCode);
            if (language == null)
            {
                Logger.Error($"Language {languageCode} not found");
                return;
            }

            await new LanguageHandler().SwitchGameLanguage(language.Value);
            _currentLanguage = language.Value;
        }

        private async Task SwitchGameLanguage(Language language)
        {
            if (_currentLanguage.Code == language.Code) return;
            Logger.Debug("Switching game language to " + language.Code);
            var modsPath = Path.Combine(AppSettings.GetGamePath(), @"BLUEPROTOCOL\BLUEPROTOCOL\Content\Paks\~mods");
            Directory.CreateDirectory(modsPath);

            // When loading the Japanese language, just delete the pak and sig file and return
            if (language.Code == "ja_jp")
            {
                File.Delete(Path.Combine(modsPath, "blast_translation_mod_1_P.pak"));
                File.Delete(Path.Combine(modsPath, "blast_translation_mod_1_P.sig"));
                return;
            }

            var pakFile = Path.Combine(modsPath, language.PakFile.Split('/').Last());
            var sigFile = Path.Combine(modsPath, language.SigFile.Split('/').Last());

            await CopyResourceToFile(language.PakFile, pakFile);
            await CopyResourceToFile(language.SigFile, sigFile);
        }

        private static async Task CopyResourceToFile(string resourcePath, string destinationPath)
        {
            Logger.Debug($"Copying resource {resourcePath} to {destinationPath}");
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                foreach (var resource in assembly.GetManifestResourceNames())
                {
                    Logger.Debug($"Available resource: {resource}");
                }

                var resourceUri = new Uri(resourcePath, UriKind.RelativeOrAbsolute);
                var resourceStream = Application.GetResourceStream(resourceUri)?.Stream;

                if (resourceStream == null)
                {
                    Logger.Error($"Failed to load resource {resourcePath}");
                    return;
                }

                await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
                await resourceStream.CopyToAsync(fileStream);
                Logger.Debug($"Successfully copied {resourcePath} to {destinationPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error copying resource {resourcePath} to {destinationPath}: {ex.Message}");
            }
        }

        public List<Language> GetLanguages()
        {
            return _languages;
        }
    }
}