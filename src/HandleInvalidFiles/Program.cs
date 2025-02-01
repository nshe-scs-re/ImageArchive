var rootDirectoryPath = @"C:\Users\whaley\source\Images";

var invalidDirectoryPath = @"C:\Users\whaley\source\invalid";

if(!Directory.Exists(rootDirectoryPath))
{
    Console.WriteLine($"[ERROR] [Program.cs] [Main]: '{rootDirectoryPath}' is an invalid directory path.");
    return;
}

if(!Directory.Exists(invalidDirectoryPath))
{
    Directory.CreateDirectory(invalidDirectoryPath);
}

Console.WriteLine($"[INFO] [Program.cs] [Main]: The root directory is set to: '{rootDirectoryPath}'");
Console.WriteLine($"[INFO] [Program.cs] [Main]: The invalid file directory is set to: '{invalidDirectoryPath}'");

Console.WriteLine("[INFO] [Program.cs] [Main]: Fetching file paths...");
var filePaths = GetFilePaths(rootDirectoryPath);

var acceptedFileExtensions = new List<string> { ".jpg", ".jpeg" };

var invalidFiles = new List<string>();

Console.WriteLine("[INFO] [Program.cs] [Main]: Searching for invalid files...");
var invalidFilecount = DetectInvalidFiles(filePaths);

if(invalidFilecount == 0)
{
    Console.WriteLine("[INFO] [Program.cs] [Main]: No invalid files found. Exiting...");
    return;
}

while(true)
{
    Console.WriteLine($"[INFO] [Program.cs] [Main]: {invalidFilecount} invalid files detected.");
    Console.WriteLine("Menu:");
    Console.WriteLine("1. List invalid files");
    Console.WriteLine("2. Move invalid files");
    Console.WriteLine("3. Copy invalid files");
    Console.WriteLine("4. Delete invalid files");
    Console.WriteLine("0. Exit");
    Console.Write("Enter your choice: ");

    var input = Console.ReadLine();

    if(int.TryParse(input, out int choice))
    {
        switch(choice)
        {
            case 0:
                return;
            case 1:
                int pageSize = 10;
                int totalFiles = invalidFiles.Count;
                int currentPage = 0;
                int maxPages = 0;

                while(currentPage * pageSize < totalFiles)
                {
                    Console.Clear();

                    var currentFiles = invalidFiles
                        .Skip(currentPage * pageSize)
                        .Take(pageSize);

                    maxPages = (int)Math.Ceiling(totalFiles / (double)pageSize);

                    Console.WriteLine($"Page {currentPage + 1}/{maxPages}");
                    Console.WriteLine("Invalid Files:");

                    foreach(var invalidFile in currentFiles)
                    {
                        Console.WriteLine(invalidFile);
                    }

                    currentPage++;

                    if(currentPage * pageSize < totalFiles)
                    {
                        Console.WriteLine("\nPress Enter to view the next page, or type 'q' to quit.");
                        input = Console.ReadLine();
                        if(input?.Trim().ToLowerInvariant() == "q")
                        {
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nEnd of list. Press Enter to return.");
                        Console.ReadLine();
                    }
                }

                break;
            case 2:
                foreach(var invalidFile in invalidFiles)
                {
                    MoveInvalidFile(invalidFile, invalidDirectoryPath);
                }

                Console.WriteLine($"[INFO] [Program.cs] [Main]: {invalidFilecount} invalid files moved to '{invalidDirectoryPath}'. Exiting...");

                return;
            case 3:
                foreach(var invalidFile in invalidFiles)
                {
                    CopyInvalidFile(invalidFile, invalidDirectoryPath);
                }

                Console.WriteLine($"[INFO] [Program.cs] [Main]: {invalidFilecount} invalid files copied to '{invalidDirectoryPath}'.");

                break;
            case 4:
                foreach(var invalidFile in invalidFiles)
                {
                    DeleteInvalidFile(invalidFile);
                }

                Console.WriteLine($"[INFO] [Program.cs] [Main]: {invalidFilecount} invalid files deleted. Exiting...");

                return;
            default:
                break;
        }
    }

}

int DetectInvalidFiles(List<string> filePaths)
{
    var invalidFilecount = 0;

    foreach(var filePath in filePaths)
    {
        var fileExtension = Path.GetExtension(filePath);
        if(!acceptedFileExtensions.Contains(fileExtension))
        {
            Console.WriteLine($"[INFO] [Program.cs] [Main]: File marked invalid because of extension: {filePath}");
            invalidFiles.Add(filePath);
            invalidFilecount++;
            continue;
        }

        string fileName = Path.GetFileName(filePath);

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        if(fileNameWithoutExtension.Length <= 2)
        {
            Console.WriteLine($"[INFO] [Program.cs] [Main]: File with short name marked invalid: {filePath}");
            invalidFiles.Add(filePath);
            invalidFilecount++;
            continue;
        }

        string unixTimeString = fileNameWithoutExtension[..^2];

        if(!long.TryParse(unixTimeString, out long unixTime))
        {
            Console.WriteLine($"[INFO] [Program.cs] [Main]: File with invalid Unix time marked invalid: {filePath}");
            invalidFiles.Add(filePath);
            invalidFilecount++;
            continue;
        }

        try
        {
            using(FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using(BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    if(fileStream.Length < 2)
                    {
                        Console.WriteLine($"[INFO] [Program.cs] [Main]: File marked invalid: {filePath}");
                        invalidFiles.Add(filePath);
                        invalidFilecount++;
                        continue;
                    }

                    fileStream.Position = fileStream.Length - 2;

                    ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

                    if(segmentMarker != 0xFFD9) // JPEG EOI marker (0xFFD9)
                    {
                        Console.WriteLine($"[INFO] [Program.cs] [Main]: File marked invalid: {filePath}");
                        invalidFiles.Add(filePath);
                        invalidFilecount++;
                        continue;
                    }

                    fileStream.Position = 0;

                    ushort firstHeader = ConvertToBigEndian16(binaryReader.ReadUInt16());

                    if(firstHeader != 0xFFD8) // JPEG SOI marker (0xFFD8)
                    {
                        Console.WriteLine($"[INFO] [Program.cs] [Main]: File marked invalid: {filePath}");
                        invalidFiles.Add(filePath);
                        invalidFilecount++;
                        continue;
                    }
                }
            }
        }
        catch(Exception)
        {
            Console.WriteLine($"[ERROR] [Program.cs] [Main]: An error occurred while reading file: {filePath}.");
            throw;
        }
    }

    return invalidFilecount;
}

void MoveInvalidFile(string absoluteFilePath, string invalidFileDirectoryPath)
{

    if(!Path.IsPathFullyQualified(absoluteFilePath))
    {
        Console.WriteLine($"[ERROR] [Program.cs] [MoveInvalidFile]: Not a fully qualified file path: {absoluteFilePath}");
        return;
    }

    if(!Directory.Exists(invalidFileDirectoryPath))
    {
        Console.WriteLine($"[INFO] [Program.cs] [MoveInvalidFile]: Creating directory at path: {invalidFileDirectoryPath}");
        Directory.CreateDirectory(invalidFileDirectoryPath);
    }

    var fileName = Path.GetFileName(absoluteFilePath);

    var destinationPath = Path.Combine(invalidFileDirectoryPath, fileName);

    try
    {
        File.Move(absoluteFilePath, destinationPath);
        Console.WriteLine($"[INFO] [Program.cs] [MoveInvalidFile] File at path '{absoluteFilePath}' moved to '{destinationPath}'");
    }
    catch(Exception)
    {
        Console.WriteLine($"[ERROR] [Program.cs] [MoveInvalidFile]: An error occurred while moving file: {absoluteFilePath}");
        throw;
    }
}

void CopyInvalidFile(string absoluteFilePath, string invalidFileDirectoryPath)
{

    if(!Path.IsPathFullyQualified(absoluteFilePath))
    {
        Console.WriteLine($"[ERROR] [Program.cs] [CopyInvalidFile]: Not a fully qualified file path: {absoluteFilePath}");
        return;
    }

    if(!Directory.Exists(invalidFileDirectoryPath))
    {
        Console.WriteLine($"[INFO] [Program.cs] [MoveInvalidFile]: Creating directory at path: {invalidFileDirectoryPath}");
        Directory.CreateDirectory(invalidFileDirectoryPath);
    }

    var fileName = Path.GetFileName(absoluteFilePath);

    var destinationPath = Path.Combine(invalidFileDirectoryPath, fileName);

    try
    {
        File.Copy(absoluteFilePath, destinationPath, overwrite: true);
        Console.WriteLine($"[INFO] [Program.cs] [CopyInvalidFile] File at path '{absoluteFilePath}' copied to '{destinationPath}'");
    }
    catch(Exception)
    {
        Console.WriteLine($"[ERROR] [Program.cs] [CopyInvalidFile]: An error occurred while copying file: {absoluteFilePath}");
        throw;
    }
}

void DeleteInvalidFile(string absoluteFilePath)
{
    if(!Path.IsPathFullyQualified(absoluteFilePath))
    {
        Console.WriteLine($"[ERROR] [Program.cs] [DeleteInvalidFile]: '{absoluteFilePath}' is not a fully qualified file path.");
        return;
    }

    try
    {
        File.Delete(absoluteFilePath);
        Console.WriteLine($"[INFO] [Program.cs] [DeleteInvalidFile] File at path '{absoluteFilePath}' deleted.");
    }
    catch(Exception)
    {
        Console.WriteLine($"[ERROR] [Program.cs] [CopyInvalidFile]: An error occurred while deleting file: {absoluteFilePath}");
        throw;
    }
}

List<string> GetFilePaths(string directoryRootPath)
{
    return Directory.GetFiles(directoryRootPath, "*", SearchOption.AllDirectories).ToList();
}

ushort ConvertToBigEndian16(ushort value)
{
    ushort originalMsb = (ushort)((value >> 8) & 0xFF);
    ushort originalLsb = (ushort)(value & 0xFF);

    ushort newMsb = (ushort)(originalLsb << 8);
    ushort newLsb = originalMsb;

    return (ushort)(newMsb | newLsb);
}