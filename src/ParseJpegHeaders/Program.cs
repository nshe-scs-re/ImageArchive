using ParseJpegHeaders;

var imageDirectoryRootPath = @"C:\Users\whaley\source\Images";

if(!Directory.Exists(imageDirectoryRootPath))
{
    Console.WriteLine($"[ERROR] [Program.cs] [Main]: '{nameof(imageDirectoryRootPath)}' is an invalid directory path. Exiting...");
    return;
}

Console.WriteLine("[INFO] [Program.cs] [Main]: Fetching image paths...");
var imagePaths = GetImagePaths(imageDirectoryRootPath);

var invalidImages = new List<string>();
var byteMapping = new Dictionary<string, int>();

Console.WriteLine("[INFO] [Program.cs] [Main]: Parsing image headers...");
for(int i = 0; i < imagePaths.Count; i++)
{
    try
    {
        if(Path.IsPathFullyQualified(imagePaths[i]) && File.Exists(imagePaths[i]))
        {
            using(FileStream fileStream = new FileStream(imagePaths[i], FileMode.Open, FileAccess.Read))
            {
                using(BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if(!JpegIsValid(fileStream, binaryReader))
                    {
                        return;
                    }

                    while(fileStream.Position < fileStream.Length)
                    {
                        ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

                        ushort segmentLength = ConvertToBigEndian16(binaryReader.ReadUInt16());

                        if(segmentLength < 2 || (fileStream.Position + (segmentLength - 2)) > fileStream.Length)
                        {
                            Console.WriteLine("[ERROR] [Program.cs] [Main]: Invalid segment length. Exiting...");
                            return;
                        }

                        if(segmentMarker == 0xFFE0) // APP0 marker (0xFFE0)
                        {
                            byte[] app0_header = binaryReader.ReadBytes(segmentLength - 2);

                            byte byte_27 = app0_header[26];
                            byte byte_29 = app0_header[28];

                            if(byte_27 == byte_29)
                            {
                                int byte_27_decimal_value = byte_27;

                                imagePaths[i] = ConvertSingleWindowsPathToLinuxPath(imagePaths[i]);

                                if(!byteMapping.TryAdd(imagePaths[i], byte_27_decimal_value))
                                {
                                    Console.WriteLine($"[ERROR] [Program.cs] [Main]: Could not add image with path '{imagePaths[i]}' to byte mapping. Exiting...");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[ERROR] [Program.cs] [Main]: Byte 27 and Byte 29 do not match. Skipping file at path '{imagePaths[i]}'");
                            }

                            break;
                        }
                        else
                        {
                            Console.WriteLine("[INFO] [Program.cs] [Main]: Found segment other than APP0, skipping segment.");
                            fileStream.Position += segmentLength - 2;
                        }
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"[ERROR] [Program.CS] [Main]: '{imagePaths[i]}' is an invalid file path.");
        }
    }
    catch(Exception)
    {
        throw;
    }
}

Console.WriteLine($"[INFO] [Program.cs] [Main]: Parsing complete. {byteMapping.Count} images parsed.");

Console.WriteLine($"[INFO] [Program.cs] [Main]: Attempting to save to database...");

using(var dbContext = new ImageDbContext())
{
    Dictionary<int, string> RocklandNameMap = new Dictionary<int, string>
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
    };

    Dictionary<int, string> SheepOneNameMap = new Dictionary<int, string>
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
    };

    Dictionary<int, string> SheepTwoNameMap = new Dictionary<int, string>
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
    };

    Dictionary<int, string> SheepThreeNameMap = new Dictionary<int, string>
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
    };
    Dictionary<int, string> SheepFourNameMap = new Dictionary<int, string>
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
    };
    Dictionary<int, string> SpringZeroNameMap = new Dictionary<int, string>
    {
        { 1, "North" },
        { 2, "North East" },
        { 3, "East"},
        { 4, "South East" },
        { 5, "South" },
        { 6, "South West" },
        { 7, "West" },
        { 8, "North West" },
    };
    Dictionary<int, string> SpringOneNameMap = new Dictionary<int, string>
    {
        { 1, "North" },
        { 2, "North East" },
        { 3, "East"},
        { 4, "South East" },
        { 5, "South" },
        { 6, "South West" },
        { 7, "West" },
        { 8, "North West" },
    };
    Dictionary<int, string> SpringTwoNameMap = new Dictionary<int, string>
    {
        { 1, "North" },
        { 2, "North East" },
        { 3, "East"},
        { 4, "South East" },
        { 5, "South" },
        { 6, "South West" },
        { 7, "West" },
        { 8, "North West" },
    };
    Dictionary<int, string> SpringThreeNameMap = new Dictionary<int, string>
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
    };
    Dictionary<int, string> SpringFourNameMap = new Dictionary<int, string>
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
    };

    Dictionary<int, string> SnakeOneNameMap = new Dictionary<int, string>
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
        { 15, "Great Basin National Park"},
    };
    Dictionary<int, string> SnakeTwoNameMap = new Dictionary<int, string>
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
        { 15, "Spring One"},
    };

    Dictionary<int, string> SnakeThreeNameMap = new Dictionary<int, string>
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
    };

    Dictionary<int, string> EldoradoThreeNameMap = new Dictionary<int, string>
    {
        { 1, "North" },
        { 2, "North East" },
        { 3, "East"},
        { 4, "South East" },
        { 5, "South" },
        { 6, "South West" },
        { 7, "West" },
        { 8, "North West" },
    };

    try
    {
        var paths = byteMapping.Keys.ToList();
        var images = dbContext.Images.Where(i => paths.Contains(i.FilePath)).ToList();

        foreach(var image in images)
        {
            if(byteMapping.TryGetValue(image.FilePath, out int cameraPosition))
            {
                image.CameraPositionNumber = cameraPosition;

                switch(image.SiteName)
                {
                    case "Rockland":
                        image.CameraPositionName = RocklandNameMap.GetValueOrDefault(cameraPosition);
                        break;
                    case "Sheep":
                        switch(image.SiteNumber)
                        {
                            case 1:
                                image.CameraPositionName = SheepOneNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 2:
                                image.CameraPositionName = SheepTwoNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 3:
                                image.CameraPositionName = SheepThreeNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 4:
                                image.CameraPositionName = SheepFourNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            default:
                                break;
                        }
                        break;
                    case "Spring":
                        switch(image.SiteNumber)
                        {
                            case 0:
                                image.CameraPositionName = SpringZeroNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 1:
                                image.CameraPositionName = SpringOneNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 2:
                                image.CameraPositionName = SpringTwoNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 3:
                                image.CameraPositionName = SpringThreeNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 4:
                                image.CameraPositionName = SpringFourNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            default:
                                break;
                        }
                        break;
                    case "Snake":
                        switch(image.SiteNumber)
                        {
                            case 1:
                                image.CameraPositionName = SnakeOneNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 2:
                                image.CameraPositionName = SnakeTwoNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            case 3:
                                image.CameraPositionName = SnakeThreeNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            default:
                                break;
                        }
                        break;
                    case "Eldorado":
                        switch(image.SiteNumber)
                        {
                            case 1:
                                break;
                            case 2:
                                break;
                            case 3:
                                image.CameraPositionName = EldoradoThreeNameMap.GetValueOrDefault(cameraPosition);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Console.WriteLine($"[ERROR] [Program.cs] [Main]: Could not find camera position for image with path '{image.FilePath}'");
            }
        }

        dbContext.SaveChanges();
        Console.WriteLine("[INFO] [Program.cs] [Main]: Camera positions saved to database. Exiting...");
    }
    catch(Exception)
    {
        throw;
    }
}

string ConvertSingleWindowsPathToLinuxPath(string path)
{
    string windowsBasePath = @"C:\Users\whaley\source";
    string linuxBasePath = @"/app";

    return path.Replace(windowsBasePath, linuxBasePath).Replace('\\', '/');
}

ushort ConvertToBigEndian16(ushort value)
{
    ushort originalMsb = (ushort)((value >> 8) & 0xFF);
    ushort originalLsb = (ushort)(value & 0xFF);

    ushort newMsb = (ushort)(originalLsb << 8);
    ushort newLsb = originalMsb;

    return (ushort)(newMsb | newLsb);
}

List<string> GetImagePaths(string directoryRootPath)
{
    List<string> imagePaths = new List<string>();

    List<string> imageExtensions = [".jpg", ".jpeg"];

    foreach(var extension in imageExtensions)
    {
        try
        {
            imagePaths.AddRange(Directory.GetFiles(directoryRootPath, $"*{extension}", SearchOption.AllDirectories));
        }
        catch(Exception)
        {
            throw;
        }
    }

    return imagePaths;
}

bool JpegIsValid(FileStream fileStream, BinaryReader binaryReader)
{
    if(fileStream.Length < 2)
    {
        Console.WriteLine($"[INFO] [Program.cs] [Main]: File too small to be a valid JPEG file. Exiting...");
        return false;
    }

    fileStream.Position = fileStream.Length - 2;

    ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

    if(segmentMarker != 0xFFD9) // JPEG EOI marker (0xFFD9)
    {
        Console.WriteLine($"[ERROR] [Program.cs] [Main]: Unexpected EOI marker: 0x{segmentMarker:X4} Exiting...");
        return false;
    }

    fileStream.Position = 0;

    ushort firstHeader = ConvertToBigEndian16(binaryReader.ReadUInt16());

    if(firstHeader != 0xFFD8) // JPEG SOI marker (0xFFD8)
    {
        Console.WriteLine($"[ERROR] [Program.cs] [Main]: Unexpected SOI marker: 0x{firstHeader:X4} Exiting...");
        return false;
    }

    return true;
}