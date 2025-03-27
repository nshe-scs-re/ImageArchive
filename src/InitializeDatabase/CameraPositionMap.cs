namespace InitializeDatabase;
public class Site
{
    public required string Name { get; init; }
    public required Dictionary<int, SubSite> SubSites { get; init; }
}

public class SubSite
{
    public required string Name { get; init; }
    public required Dictionary<int, string> NameMap{ get; init; }
}

public static class CameraPositionMap
{
    public static readonly Dictionary<string, Site> Sites = new()
    {
        { "sheep", new Site {
            Name = "sheep",
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
        { "rockland", new Site {
            Name = "rockland",
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
        { "spring", new Site {
            Name = "spring",
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
        {"snake", new Site { 
            Name = "snake",
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
                        { 12, "?Tower Base 2"},
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
        {"eldorado", new Site {
            Name = "eldorado",
            SubSites = new Dictionary<int, SubSite>
            {
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

    public static string? GetCameraPositionName(string? siteName, int siteNumber, int cameraPositionNumber)
    {
        if(siteName == null)
        {
            return null;
        }

        return Sites.TryGetValue(siteName, out var site)
            && site.SubSites.TryGetValue(siteNumber, out var subSite)
            && subSite.NameMap.TryGetValue(cameraPositionNumber, out var positionName)
            ? positionName
            : null;
    }
}
