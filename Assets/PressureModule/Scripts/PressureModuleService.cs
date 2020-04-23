using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class PressureModuleService : MonoBehaviour
{

    private string _settingsFile;
    private PressureModuleSettings _settings;

    void Start()
    {
        name = "Pressure Service";

        _settingsFile = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "PressureSettings.json");

        if (!File.Exists(_settingsFile))
            _settings = new PressureModuleSettings();
        else
        {
            try
            {
                _settings = JsonConvert.DeserializeObject<PressureModuleSettings>(File.ReadAllText(_settingsFile), new StringEnumConverter());
                if (_settings == null)
                    throw new Exception("Settings could not be read. Creating new Settings...");
                Debug.LogFormat(@"[Pressure Service] Settings successfully loaded");
            }
            catch (Exception e)
            {
                Debug.LogFormat(@"[Pressure Service] Error loading settings file:");
                Debug.LogException(e);
                _settings = new PressureModuleSettings();
            }
        }

        Debug.LogFormat(@"[Pressure Service] Service is active");
        StartCoroutine(GetData());
    }

    public string[] GetAuthors(string moduleId)
    {
        string[] setting;
        _settings.RememberedAuthors.TryGetValue(moduleId, out setting);
        return setting ?? new string[0];
    }

    public DateTime GetReleaseDate(string moduleId)
    {
        if (!_settings.RememberedReleaseDates.ContainsKey(moduleId)) return DateTime.Now;
        DateTime setting;
        _settings.RememberedReleaseDates.TryGetValue(moduleId, out setting);
        return setting;
    }

    IEnumerator GetData()
    {
        using (var http = UnityWebRequest.Get(_settings.SiteUrl))
        {
            // Request and wait for the desired page.
            yield return http.SendWebRequest();

            if (http.isNetworkError)
            {
                Debug.LogFormat(@"[Pressure Service] Website {0} responded with error: {1}", _settings.SiteUrl, http.error);
                yield break;
            }

            if (http.responseCode != 200)
            {
                Debug.LogFormat(@"[Pressure Service] Website {0} responded with code: {1}", _settings.SiteUrl, http.responseCode);
                yield break;
            }

            var allModules = JObject.Parse(http.downloadHandler.text)["KtaneModules"] as JArray;
            if (allModules == null)
            {
                Debug.LogFormat(@"[Pressure Service] Website {0} did not respond with a JSON array at “KtaneModules” key.", _settings.SiteUrl, http.responseCode);
                yield break;
            }

            var authors = new Dictionary<string, string[]>();
            var releaseDates = new Dictionary<string, DateTime>();

            foreach (JObject module in allModules)
            {
                var id = module["ModuleID"] as JValue;
                if (id == null || !(id.Value is string))
                    continue;
                var author = module["Author"] as JValue;
                if (author == null || !(author.Value is string))
                    continue;
                authors[(string)id.Value] = ((string)author.Value).Split(',');
                var releaseDateString = module["Published"] as JValue;
                DateTime releaseDate;
                if (!DateTime.TryParse((string)releaseDateString.Value, out releaseDate))
                    continue;
                releaseDates[(string)id.Value] = releaseDate;
            }

            Debug.LogFormat(@"[Pressure Service] List successfully loaded:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, authors.Select(kvp => string.Format("[Pressure Service] {0} => {1}", kvp.Key, kvp.Value)).ToArray()));
            _settings.RememberedAuthors = authors;
            _settings.RememberedReleaseDates = releaseDates;

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(_settingsFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));
                File.WriteAllText(_settingsFile, JsonConvert.SerializeObject(_settings, Formatting.Indented, new StringEnumConverter()));
            }
            catch (Exception e)
            {
                Debug.LogFormat("[Pressure Service] Failed to save settings file:");
                Debug.LogException(e);
            }
        }
    }
}
