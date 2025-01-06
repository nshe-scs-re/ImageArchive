using System;
using System.IO;

string filePath = @"C:\ImagesTest\Sheep\Site 1\Images\17840\154140841683.jpg";

if(Path.IsPathFullyQualified(filePath) && File.Exists(filePath))
{
    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    using (BinaryReader binaryReader = new BinaryReader(fileStream))
    {
        ushort firstHeader = ConvertToBigEndian16(binaryReader.ReadUInt16());

        if (firstHeader != 0xFFD8) // JPEG SOI marker (0xFFD8)
        {
            Console.WriteLine($"[ERROR] [Program.cs] [Main]: Unexpected SOI marker: 0x{firstHeader:X4}");
            Console.WriteLine("[ERROR] [Program.cs] [Main]: Not a valid JPEG file. Exiting...");
            return;
        }

        while (fileStream.Position < fileStream.Length)
        {
            ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

            ushort segmentLength = ConvertToBigEndian16(binaryReader.ReadUInt16());

            if (segmentMarker == 0xFFE0) // APP0 marker (0xFFE0)
            {
                byte[] app0_header = binaryReader.ReadBytes(60);

                Console.WriteLine("[INFO] [Program.cs] [Main]: Found APP0 header. Relevant bytes:");

                Console.WriteLine("----------------------------------");
                Console.WriteLine("{0, -3} | {1, -10} | {2, -15}", "Byte", "Hex Value", "Decimal Value");
                Console.WriteLine("----------------------------------");

                byte byte_27 = app0_header[26];
                byte byte_29 = app0_header[28];

                Console.WriteLine("{0, -4} | 0x{1,-8:X2} | {1,-15}", 27, byte_27);
                Console.WriteLine("{0, -4} | 0x{1,-8:X2} | {1,-15}", 29, byte_29);

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
else
{
    Console.WriteLine($"[ERROR] [Program.CS] [Main]: '{filePath}' is an invalid file path.");
}

ushort ConvertToBigEndian16(ushort value)
{
    ushort originalMsb = (ushort)((value >> 8) & 0xFF);
    ushort originalLsb = (ushort)(value & 0xFF);

    ushort newMsb = (ushort)(originalLsb << 8);
    ushort newLsb = originalMsb;

    return (ushort)(newMsb | newLsb);
}
