using System;
using System.Collections.Generic;

public sealed class PressureModuleSettings
{
    public string SiteUrl = @"https://ktane.timwi.de/json/raw";

    public Dictionary<string, string[]> RememberedAuthors = new Dictionary<string, string[]>();
    public Dictionary<string, DateTime> RememberedReleaseDates = new Dictionary<string, DateTime>();

    public int Version = 1;
}