
using System.CommandLine;
using System.CommandLine.Parsing;
using System.ComponentModel;

string fullPath = "";
var excludedFolders = new[] { "bin", "debug", "obj", "node_moduels", "config", ".git", ".vs" };
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
var rspCommand = new Command("create-rsp", "create rsp file");

// option
var bundleOption = new Option<FileInfo>(new[] { "--output", "-o" }, "File path and name");
var sourceOption = new Option<bool>(new[] { "--source", "-s" }, "Add source path to output file");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "Add author to output file");
var languageOption = new Option<string[]>(new[] { "--language", "-l" }, "Add list of language");
var sortOption = new Option<bool>("--sort", "sort files by files type");
var removeOption = new Option<bool>(new[] { "--remove", "-r" }, "remove empty lines");
languageOption.AllowMultipleArgumentsPerToken = true;
languageOption.IsRequired = true;

bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(sourceOption);
bundleCommand.AddOption(authorOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeOption);

string[] languagesArray;

rspCommand.SetHandler(() =>
{
    Console.WriteLine("Enter the programming languages (comma-separated or 'all'):");
    string languages = Console.ReadLine() ?? "all";
    if (languages.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
        languagesArray = new[] { "all" };
    }
    else
    {
        languagesArray = languages.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    Console.WriteLine("Enter the output file name (e.g., bundle.txt):");
    string outputFile = Console.ReadLine() ?? "bundle.txt";

    Console.WriteLine("Include source path in the bundle? (y/n):");
    string includeSource = Console.ReadLine()?.ToLower() == "y" ? "-s" : "";

    Console.WriteLine("Sort files by type? (y/n):");
    string sortOption = Console.ReadLine() == "y" ? "--sort" : "";

    Console.WriteLine("Remove empty lines? (y/n):");
    string removeEmptyLines = Console.ReadLine()?.ToLower() == "y" ? "--remove" : "";

    Console.WriteLine("Enter the author name (optional):");
    string author = Console.ReadLine();
    string authorOption = !string.IsNullOrWhiteSpace(author) ? $"--author \"{author}\"" : "";

    string bundleCommand = $"bundle --language {languages} --output {outputFile} {includeSource} {sortOption} {removeEmptyLines} {authorOption}";

    string rspFileName = "response.rsp";
    File.WriteAllText(rspFileName, bundleCommand);

    Console.WriteLine($"Response file '{rspFileName}' created successfully!");
    Console.WriteLine($"To use it, run: cli @{rspFileName}");
});

bundleCommand.SetHandler((FileInfo output, bool source, string author, string[] languages, bool sort, bool remove) =>
{
    try
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string sourcePath = "";

        if (File.Exists(output.FullName))
        {
            Console.WriteLine($"Warning: The file {output.FullName} already exists and will be overwritten.");
        }

        var files = Directory.GetFiles(currentDirectory, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(file =>
                            {
                                string folder = new DirectoryInfo(Path.GetDirectoryName(file)).Name.ToLower();
                                return !excludedFolders.Contains(folder) &&
                                        (languages.Contains("all") || languages.Any(lang => file.EndsWith($".{lang}", StringComparison.OrdinalIgnoreCase)));
                            })
                            .ToList();
        if (!files.Any())
        {
            Console.WriteLine("No matching files found");
            return;
        }

        if (sort)
        {
            files = files.OrderBy(file => Path.GetExtension(file)).ThenBy(file => file).ToList();
        }
        else
        {
            files = files.OrderBy(file => Path.GetFileName(file)).ToList();
        }

        using (FileStream fs = new FileStream(output.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
        using (StreamWriter sw = new StreamWriter(fs))
        {
            try
            {
                if (!string.IsNullOrEmpty(author))
                {
                    sw.WriteLine($"author folder: {author}");

                }
                if (source)
                {
                    sw.WriteLine($"Source folder: {currentDirectory}");
                    sw.WriteLine();
                }

                foreach (var file in files)
                {
                    sw.WriteLine($"// Source file {Path.GetFileName(file)}");
                    sw.WriteLine();


                    var content = File.ReadAllLines(file);
                    if (remove)
                    {
                        content = content.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                    }
                    foreach (var line in content)
                    {
                        sw.WriteLine(line);
                    }
                    sw.WriteLine(Environment.NewLine);

                }
                Console.WriteLine($"file bundled successfuly into {output.FullName}");
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error writing to file: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Error no premissions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error: file path is invalid");
    }
}, bundleOption, sourceOption, authorOption, languageOption, sortOption, removeOption);






var rootCommand = new RootCommand("rootCommand for file bundle cli");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(rspCommand);
rootCommand.InvokeAsync(args);