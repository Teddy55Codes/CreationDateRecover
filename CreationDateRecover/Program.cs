using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;

namespace CreationDateRecover;

class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a path to an image file.");
            return;
        }

        string filePath = args[0];

        try
        {
            DateTime? creationDate = GetImageCreationDate(filePath);
            if (creationDate.HasValue)
            {
                File.SetCreationTime(filePath, creationDate.Value);
                Console.WriteLine($"File creation date updated to {creationDate.Value}");
            }
            else
            {
                Console.WriteLine("Creation date not found in EXIF data.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }

    private static DateTime? GetImageCreationDate(string filePath)
    {
        using Image image = Image.FromFile(filePath);
        PropertyItem[] propItems = image.PropertyItems;
        foreach (PropertyItem propItem in propItems)
        {
            if (propItem.Id != 0x0132) continue;
            string dateTaken = System.Text.Encoding.ASCII.GetString(propItem.Value).Trim();
            DateTime creationDate;
            if (DateTime.TryParseExact(dateTaken, "yyyy:MM:dd HH:mm:ss\0", CultureInfo.InvariantCulture, DateTimeStyles.None, out creationDate))
            {
                return creationDate;
            }
        }

        return null;
    }
}