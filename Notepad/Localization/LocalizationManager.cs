using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace Notepad.Localization
{
    public class LocalizationManager
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private string _currentLanguage = "en";

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                ApplyLanguage(value);
            }
        }

        public List<string> SupportedLanguages { get; } = new() { "en", "uk" };

        public void ApplyLanguage(string lang)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri($"/Notepad;component/Localization/Strings.{lang}.xaml", UriKind.Relative)
            };

            var app = Application.Current;
            // Remove existing localization dictionaries
            for (int i = app.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var d = app.Resources.MergedDictionaries[i];
                if (d.Source?.OriginalString.Contains("/Localization/Strings.") == true)
                {
                    app.Resources.MergedDictionaries.RemoveAt(i);
                }
            }
            app.Resources.MergedDictionaries.Add(dict);
            _currentLanguage = lang;
        }

        public static string Get(string key)
        {
            if (Application.Current.Resources.Contains(key))
                return Application.Current.Resources[key]?.ToString() ?? key;
            return key;
        }
    }
}
