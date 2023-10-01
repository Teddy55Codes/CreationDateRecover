using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;
using CommandLine;

namespace CreationDateRecover;

public class CreationDateRecover
{
    private class Options
    {
        [Option('t', "target", Required = false, HelpText = "Path to the image file or directory.")]
        public string TargetPath { get; set; }
        
        [Option('r', "recursive", Required = false, HelpText = "Whether or not subfolders are processed (true/false). Only does something if the target is a directory.")]
        public bool IsRecursive { get; set; }
    }
    
    public static void Main(string[] args)
    {
        var nullableOptions = ParseArguments(args);
        if (nullableOptions is not {} options) return;

        switch (options.TargetPath)
        {
            case string tp when Directory.Exists(tp):
                SetImageCreationDateOnDirectory(options.TargetPath, options.IsRecursive);
                break;
            case string tp when File.Exists(tp):
                SetImageCreationDateOnFile(options.TargetPath);
                break;
        }
    }
    
    private static Options? ParseArguments(string[] args)
    {
        var result = Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                if (string.IsNullOrEmpty(o.TargetPath))
                {
                    Console.WriteLine("need to set a target with -t/--target");
                    throw new ArgumentException();
                }
                if (!File.Exists(o.TargetPath) && !Directory.Exists(o.TargetPath))
                {
                    Console.WriteLine($"\"{o.TargetPath}\" is nether a file nor a directory.");
                    throw new ArgumentException();
                }
            });
        
        return result.Errors.Any() ? null : result.Value;
    }

    private static void SetImageCreationDateOnDirectory(string directoryPath, bool isRecursive)
    {
        foreach (string subItem in Directory.GetFileSystemEntries(directoryPath))
        {
            switch (subItem)
            {
                case string si when Directory.Exists(si) && isRecursive:
                    SetImageCreationDateOnDirectory(subItem, isRecursive);
                    break;
                case string si when File.Exists(si):
                    SetImageCreationDateOnFile(subItem);
                    break;
            }
        }
    }

    private static void SetImageCreationDateOnFile(string filePath)
    {
        try
        {
            DateTime? creationDate = GetImageCreationDate(filePath);
            
            if (creationDate.HasValue)
            {
                File.SetCreationTime(filePath, creationDate.Value);
                Console.WriteLine($"Creation date updated for {filePath} to {creationDate.Value}");
            }
            else
            {
                Console.WriteLine($"Creation date not found in EXIF data from file {filePath}.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message} for file {filePath}");
        }
    }

    private static DateTime? GetImageCreationDate(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        
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