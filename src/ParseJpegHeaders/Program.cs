using ParseJpegHeaders;

var imageDirectoryRootPath = @"";

if(!Directory.Exists(imageDirectoryRootPath))
{
    Console.WriteLine($"[ERROR] [Program.cs] [Main]: '{imageDirectoryRootPath}' is an invalid directory path. Exiting...");
    return;
}

Console.WriteLine("[INFO] [Program.cs] [Main]: Fetching image paths...");
var imagePaths = GetImagePaths(imageDirectoryRootPath);

var invalidImages = new List<string>();
var byteMapping = new Dictionary<string, int>();

Console.WriteLine("[INFO] [Program.cs] [Main]: Parsing image headers...");
for(int i=0; i<imagePaths.Count; i++)
{
    try
    {
        if(Path.IsPathFullyQualified(imagePaths[i]) && File.Exists(imagePaths[i]))
        {
            using (FileStream fileStream = new FileStream(imagePaths[i], FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if(!JpegIsValid(fileStream, binaryReader))
                    {
                        return;
                    }

                    while (fileStream.Position < fileStream.Length)
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
    try
    {
        var paths = byteMapping.Keys.ToList();
        var images = dbContext.Images.Where(i => paths.Contains(i.FilePath)).ToList();

        foreach(var image in images)
        {
            if(byteMapping.TryGetValue(image.FilePath, out int cameraPosition))
            {
                image.CameraPosition = cameraPosition;
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
    string windowsBasePath = @"";
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