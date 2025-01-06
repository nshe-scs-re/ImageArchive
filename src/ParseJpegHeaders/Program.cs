using System;
using System.IO;

string filePath = @"C:\ImagesTest\Sheep\Site 1\Images\17840\154140841683.jpg";

if(Path.IsPathFullyQualified(filePath) && File.Exists(filePath))
{
    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    using (BinaryReader binaryReader = new BinaryReader(fileStream))
    {
        // Read the first 2 bytes and reverse the byte order to account for endianness
        //ushort temp = (ushort)((br.ReadByte() << 8) | br.ReadByte());

        ushort firstHeader = ConvertToBigEndian16(binaryReader.ReadUInt16());

        // Check if it's the JPEG SOI marker (0xFFD8)
        if (firstHeader != 0xFFD8)
        {
            Console.WriteLine($"Unexpected SOI marker: 0x{firstHeader:X4}");
            Console.WriteLine("Not a valid JPEG file.");
            return;
        }

        while (fileStream.Position < fileStream.Length)
        {
            // Read segment marker (reverse the byte order)
            //ushort marker = (ushort)((binary_reader.ReadByte() << 8) | binary_reader.ReadByte());
            ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

            // Read segment length (reverse the byte order)
            //ushort length = (ushort)((binary_reader.ReadByte() << 8) | binary_reader.ReadByte());
            ushort segmentLength = ConvertToBigEndian16(binaryReader.ReadUInt16());

            if (segmentMarker == 0xFFE0) // APP0 marker
            {
                Console.WriteLine("Found APP0 segment!");

                // Read the entire 60 bytes of the APP0 header
                byte[] app0_header = binaryReader.ReadBytes(60);

                Console.WriteLine("APP0 Header in Hex:");
                Console.WriteLine(BitConverter.ToString(app0_header).Replace("-", " "));

                // Extract and display the values of bytes 27 and 29
                byte byte_27 = app0_header[26];
                byte byte_29 = app0_header[28];

                Console.WriteLine($"Value of byte 27: 0x{byte_27:X2} ({byte_27})");
                Console.WriteLine($"Value of byte 29: 0x{byte_29:X2} ({byte_29})");

                break;
            }
            else
            {
                // Skip the segment
                Console.WriteLine("Skipping segment.");
                fileStream.Position += segmentLength - 2;
            }
        }
    }
}
else
{
    Console.WriteLine($"[ERROR] [Program.CS] [Main] '{filePath}' is an invalid file path.");
}

ushort ConvertToBigEndian16(ushort value)
{
    ushort originalMsb = (ushort)((value >> 8) & 0xFF);
    ushort originalLsb = (ushort)(value & 0xFF);

    ushort newMsb = (ushort)(originalLsb << 8);
    ushort newLsb = originalMsb;

    return (ushort)(newMsb | newLsb);
}
