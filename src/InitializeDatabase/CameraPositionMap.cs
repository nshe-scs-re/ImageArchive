namespace InitializeDatabase;
public class Site
{
    public required string Name { get; init; }
    public required Dictionary<int, SubSite> SubSites { get; init; }
}

public class SubSite
{
    public required string Name { get; init; }
    public required Dictionary<int, string> NameMap { get; init; }
}

public static class CameraPositionMap
{
    public static readonly Dictionary<string, Site> Sites = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Sheep", new Site {
            Name = "Sheep",
            SubSites = new Dictionary<int, SubSite>
            {
                { 1, new SubSite {
                    Name = "site_1",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Soil Sensors"},
                        { 13, "Precipitation Gage"},
                        { 14, "Angel Peak"},
                        { 15, "Refuge HQ"},
                        { 16, "Vegetation Interspace"}
                    }
                }
                },
                { 2, new SubSite {
                    Name = "site_2",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Soil Sensors"},
                        { 13, "Precipitation Gage"},
                        { 14, "LV Precip Station"},
                        { 15, "Angel Peak"},
                        { 16, "Hayford Peak"},
                        { 17, "Sheep 3&4"},
                        { 18, "Canopy Interspace"}
                    }
                }
                },
                { 3, new SubSite {
                    Name = "site_3",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Soil Sensors"},
                        { 13, "Precipitation Gage"},
                        { 14, "Runoff Collector"},
                        { 15, "NRCS SCAN Site"},
                        { 16, "LV Precip Station"}
                    }
                }
                },
                { 4, new SubSite {
                    Name = "site_4",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Soil Sensors"},
                        { 13, "Precipitation Gage"},
                        { 14, "Rain Gage"},
                        { 15, "Canopy Interspace"},
                        { 16, "Sap Flow Sensors"},
                        { 17, "PIMO Canopy"}
                    }
                }
                }
            }
        }
        },
        { "Rockland", new Site {
            Name = "Rockland",
            SubSites = new Dictionary<int, SubSite>
            {
                {1, new SubSite {
                    Name = "site_1",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Mason Valley"},
                        { 12, "Sawtooth Ridge"},
                        { 13, "Sweetwater Range"},
                        { 14, "White Mountain Range"},
                        { 15, "Walker River"},
                        { 16, "Corey Peak"},
                        { 17, "Mt Grant"},
                        { 18, "North Aspect"},
                        { 19, "South Aspect"},
                        { 20, "Mt Rose"}
                    }
                    }
                }
            }
        }
        },
        { "Spring", new Site {
            Name = "Spring",
            SubSites = new Dictionary<int, SubSite>
            {
                {0, new SubSite {
                    Name = "site_0",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" }
                    }
                }
                },
                {1, new SubSite {
                    Name = "site_1",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" }
                    }
                }
                },
                {2, new SubSite {
                    Name = "site_2",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" }
                    }
                }
                },
                {3, new SubSite {
                    Name = "site_3",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Soil Sensors"},
                        { 13, "Precipitation Gage"},
                        { 14, "Snow Pole 1"},
                        { 15, "Snow Pole 2"},
                        { 16, "Mountain Mahogony Canopy"},
                        { 17, "Spring 1"},
                        { 18, "Cave Mountain"}
                    }
                }
                },
                {4, new SubSite {
                    Name = "site_4",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Soil Sensors"},
                        { 13, "Precipitation Gage"},
                        { 14, "Snow Pole"},
                        { 15, "Snow Weighing Sensors (Shade)"},
                        { 16, "Snow Weighing Sensors (Sun)"},
                        { 17, "Bristlecone Canopy"},
                        { 18, "Limber Pine Canopy"},
                        { 19, "South Spring Valley"},
                        { 20, "South Schell Creek Range"}
                    }
                }
                }
            }
        }
        },
        {"Snake", new Site {
            Name = "Snake",
            SubSites = new Dictionary<int, SubSite>
            {
                {1, new SubSite {
                    Name = "site_1",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Tower Base 2"},
                        { 13, "Precipitation Gage"},
                        { 14, "UNLV Data Logger"},
                        { 15, "Great Basin National Park"}
                    }
                }
                },
                {2, new SubSite {
                    Name = "site_2",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Soil Sensors"},
                        { 13, "Precipitation Gage"},
                        { 14, "Great Basin Ranch Exhibit"},
                        { 15, "Spring One"}
                    }
                }
                },
                {3, new SubSite {
                    Name = "site_3",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Precipitation Gage"},
                        { 13, "Sap Flow Sensors"},
                        { 14, "Vegetation Interspace"},
                        { 15, "Deciduous Leaves"},
                        { 16, "Soil Sensors"},
                        { 17, "Snow Weighing Sensor"},
                        { 18, "Snow Depth Pole"}
                    }
                }
                }
            }
        }
        },
        {"Eldorado", new Site {
            Name = "Eldorado",
            SubSites = new Dictionary<int, SubSite>
            {
                {2, new SubSite {
                    Name = "site_2",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                    }
                }
                },
                {3, new SubSite {
                    Name = "site_3",
                    NameMap = new Dictionary<int, string>
                    {
                        { 1, "North" },
                        { 2, "North East" },
                        { 3, "East"},
                        { 4, "South East" },
                        { 5, "South" },
                        { 6, "South West" },
                        { 7, "West" },
                        { 8, "North West" },
                        { 9, "Solar Panels"},
                        { 10, "Tower Base"},
                        { 11, "Site Access"},
                        { 12, "Precipitation Gage"},
                        { 13, "Sap Flow Sensors"},
                        { 14, "Vegetation Interspace"},
                        { 15, "Deciduous Leaves"},
                        { 16, "Soil Sensors"},
                        { 17, "Snow Weighing Sensor"},
                        { 18, "Snow Depth Pole"}
                    }
                }
            }
            }
        }
        }
    };

    public static List<string> GetSiteNames()
    {
        return Sites.Keys.OrderBy(name => name).ToList();
    }

    public static List<int> GetSubSiteNumbers(string siteName)
    {
        return Sites.TryGetValue(siteName, out var site)
            ? site.SubSites.Keys.OrderBy(n => n).ToList()
            : new List<int>();
    }

    public static Dictionary<int, string> GetCameraPositions(string siteName, int siteNumber)
    {
        return Sites.TryGetValue(siteName, out var site) &&
               site.SubSites.TryGetValue(siteNumber, out var subSite)
            ? subSite.NameMap
            : new Dictionary<int, string>();
    }

     public static List<int> GetCameraPositionNumbers(string siteName, int siteNumber)
    {
        return Sites.TryGetValue(siteName, out var site) &&
               site.SubSites.TryGetValue(siteNumber, out var subSite)
            ? subSite.NameMap.Keys.OrderBy(n => n).ToList()
            : new List<int>();
    }

    public static string? GetCameraPositionName(string? siteName, int siteNumber, int cameraPositionNumber)
    {
        return Sites.TryGetValue(siteName, out var site)
            && site.SubSites.TryGetValue(siteNumber, out var subSite)
            && subSite.NameMap.TryGetValue(cameraPositionNumber, out var positionName)
            ? positionName
            : null;
    }

    public static List<string> GetSubSiteNames(string siteName)
    {
        if(Sites.TryGetValue(siteName, out var site))
        {
            return site.SubSites.Values.Select(subSite => subSite.Name).ToList();
        }

        return new List<string>();
    }

    public static SubSite? GetSubSite(string siteName, int siteNumber)
    {
        return Sites.TryGetValue(siteName, out var site) &&
               site.SubSites.TryGetValue(siteNumber, out var subSite)
            ? subSite
            : null;
    }

    public static Dictionary<string, List<int>> GetAllSiteNumbers()
    {
        return Sites.OrderBy(kvp => kvp.Key).ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.SubSites.Keys.OrderBy(n => n).ToList(),
            StringComparer.OrdinalIgnoreCase
        );
    }
}
