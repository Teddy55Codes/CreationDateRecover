using System.Globalization;
using System.Drawing;
using System.Drawing.Imaging;
using CommandLine;

namespace CreationDateRecover;

class Program
{
    public class Options
    {
        [Option('t', "target", Required = false, HelpText = "Path to the image file or directory.")]
        public string TargetPath { get; set; }
        
        [Option('r', "recursive", Required = false, HelpText = "Whether or not subfolders are processed (true/false). Can only does something if the target is a directory.")]
        public bool IsRecursive { get; set; }
    }
    
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
                else if (!File.Exists(o.TargetPath) && !Directory.Exists(o.TargetPath))
                {
                    Console.WriteLine($"\"{o.TargetPath}\" is nether a file nor a directory.");
                    throw new ArgumentException();
                }
            })
            .WithNotParsed(HandleParseError);
        return result.Errors.Any() ? null : result.Value;
    }
    
    private static void HandleParseError(IEnumerable<Error> errs)
    {
        Console.WriteLine("Argument parsing error. Please refer to the help:");
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