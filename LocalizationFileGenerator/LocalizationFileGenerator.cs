using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

/// <summary>
/// Main class for generating localization resource files (.resx) from code files and data annotations
/// </summary>
class LocalizationFileGenerator
{
    // API configuration for translation service
    private const string baseUrl = "https://api.mymemory.translated.net/";
    private static HttpClient httpClient = new HttpClient();

    /// <summary>
    /// Main entry point for the localization generator
    /// </summary>
    static async Task Main(string[] args)
    {
        // Configure paths (currently hardcoded for testing)
        Console.WriteLine("Enter the project path:");
        string solutionPath = @"D:\Users\e_alp\source\repos\LudusAppoint"; //Console.ReadLine();

        Console.WriteLine("Enter the output path for .resx files:");
        string outputPath = @"D:\Users\e_alp\source\repos\LudusAppoint"; //Console.ReadLine();

        // Supported languages (empty string represents neutral culture)
        string[] languages = { "", "en", "tr" };

        // Get all relevant files in solution
        var files = GetFilesToScan(solutionPath);

        // Dictionary to track data annotation entries by project and resource type
        var dataAnnotationEntries = new Dictionary<string, Dictionary<string, HashSet<string>>>();

        // Process each file found in the solution
        foreach (var file in files)
        {
            await ProcessFile(file, languages, solutionPath, outputPath, dataAnnotationEntries);
        }

        // Generate .resx files for data annotation resources
        await GenerateDataAnnotationResxFiles(dataAnnotationEntries, languages, solutionPath, outputPath);
    }

    /// <summary>
    /// Retrieves all .cshtml and .cs files in the solution, excluding files starting with '_'
    /// </summary>
    static List<string> GetFilesToScan(string solutionPath)
    {
        var files = new List<string>();

        // Get view files
        var cshtmlFiles = Directory.GetFiles(solutionPath, "*.cshtml", SearchOption.AllDirectories)
                                    .Where(file => !Path.GetFileName(file).StartsWith("_"))
                                    .ToList();

        // Get code files
        var csFiles = Directory.GetFiles(solutionPath, "*.cs", SearchOption.AllDirectories)
                                .Where(file => !Path.GetFileName(file).StartsWith("_"))
                                .ToList();

        files.AddRange(cshtmlFiles);
        files.AddRange(csFiles);

        return files;
    }

    /// <summary>
    /// Processes a single file to extract localization keys and data annotations
    /// </summary>
    static async Task ProcessFile(
        string file,
        string[] languages,
        string solutionPath,
        string outputPath,
        Dictionary<string, Dictionary<string, HashSet<string>>> dataAnnotationEntries)
    {
        string content = File.ReadAllText(file);
        string relativePath = Path.GetRelativePath(solutionPath, file);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
        string projectDir = pathParts.Length > 0 ? pathParts[0] : "";

        // Regex pattern to find localization keys in code
        var localizerPattern = new Regex(@"(?:@Localizer\[""(.*?)""\]|_localizer\[""(.*?)""])", RegexOptions.IgnoreCase);
        var localizerMatches = localizerPattern.Matches(content);
        var uniqueLocalizerKeys = new HashSet<string>();

        // Extract unique localization keys from matches
        foreach (Match match in localizerMatches)
        {
            string key = string.IsNullOrEmpty(match.Groups[1].Value) ? match.Groups[2].Value : match.Groups[1].Value;
            uniqueLocalizerKeys.Add(key);
        }

        // Regex pattern to find data annotation attributes
        var dataAnnotationPattern = new Regex(
            @"ErrorMessageResourceType\s*=\s*typeof\(([^)]+)\)\s*,\s*ErrorMessageResourceName\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase
        );
        var dataAnnotationMatches = dataAnnotationPattern.Matches(content);

        // Organize data annotation keys by project and resource type
        foreach (Match match in dataAnnotationMatches)
        {
            string resourceType = match.Groups[1].Value.Trim();
            string key = match.Groups[2].Value.Trim();

            if (!dataAnnotationEntries.ContainsKey(projectDir))
                dataAnnotationEntries[projectDir] = new Dictionary<string, HashSet<string>>();

            if (!dataAnnotationEntries[projectDir].ContainsKey(resourceType))
                dataAnnotationEntries[projectDir][resourceType] = new HashSet<string>();

            dataAnnotationEntries[projectDir][resourceType].Add(key);
        }

        // Generate resource files for view/controller localization keys
        if (uniqueLocalizerKeys.Count > 0)
        {
            foreach (var language in languages)
            {
                // Construct output path in Resources directory mirroring original structure
                string[] remainingParts = pathParts.Skip(1).ToArray();
                string fileName = Path.GetFileNameWithoutExtension(file);
                string resxFileName = string.IsNullOrEmpty(language)
                    ? $"{fileName}.resx"
                    : $"{fileName}.{language}.resx";

                // Build output directory path
                string outputDir = Path.Combine(
                    outputPath,
                    projectDir,
                    "Resources",
                    Path.Combine(remainingParts.Take(remainingParts.Length - 1).ToArray())
                );

                string outputFilePath = Path.Combine(outputDir, resxFileName);
                Directory.CreateDirectory(outputDir);

                // Load existing file or create new structure
                var resxXml = File.Exists(outputFilePath)
                    ? XElement.Load(outputFilePath)
                    : CreateResxXmlStructure();

                // Add PublicResXFileCodeGenerator metadata for neutral resources
                if (string.IsNullOrEmpty(language))
                {
                    AddResXCodeGeneratorMetadata(resxXml);
                }

                // Track existing keys to prevent duplicates
                var existingKeys = new HashSet<string>(
                    resxXml.Descendants("data")
                        .Select(e => e.Attribute("name")?.Value)
                        .Where(name => !string.IsNullOrEmpty(name))
                );

                // Add new translations
                foreach (string key in uniqueLocalizerKeys)
                {
                    if (existingKeys.Contains(key)) continue;

                    string value = FormatLocalizerKey(key);
                    string translation = "";

                    // Translate only for non-source languages
                    if (!string.IsNullOrEmpty(language) && !language.Equals("en") && !language.Equals(""))
                        translation = await Translate(value, language);

                    translation = string.IsNullOrEmpty(translation) ? value : translation;

                    resxXml.Add(new XElement("data",
                        new XAttribute("name", key),
                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                        new XElement("value", translation)));
                }

                resxXml.Save(outputFilePath);
            }
        }
    }

    static void AddResXCodeGeneratorMetadata(XElement resxXml)
    {
        const string metadataName = "ResXFileCodeGenerator";

        var existingMetadata = resxXml.Elements("metadata")
            .FirstOrDefault(e => e.Attribute("name")?.Value == metadataName);

        if (existingMetadata == null)
        {
            var metadata = new XElement("metadata",
                new XAttribute("name", metadataName),
                new XAttribute("type", "System.Resources.ResXFileRef, System.Windows.Forms"),
                new XElement("value", "PublicResXFileCodeGenerator"));

            // Insert after last resheader
            var lastResHeader = resxXml.Elements("resheader").LastOrDefault();
            if (lastResHeader != null)
            {
                lastResHeader.AddAfterSelf(metadata);
            }
            else
            {
                // Fallback to adding after schema
                resxXml.Elements().First().AddAfterSelf(metadata);
            }
        }
    }

    /// <summary>
    /// Generates .resx files for data annotation resources organized by namespace
    /// </summary>
    static async Task GenerateDataAnnotationResxFiles(
        Dictionary<string, Dictionary<string, HashSet<string>>> dataAnnotationEntries,
        string[] languages,
        string solutionPath,
        string outputPath)
    {
        foreach (var projectEntry in dataAnnotationEntries)
        {
            string projectDir = projectEntry.Key;
            var resourceTypes = projectEntry.Value;

            foreach (var resourceTypeEntry in resourceTypes)
            {
                string resourceType = resourceTypeEntry.Key;
                var keys = resourceTypeEntry.Value;

                // Parse resource type namespace to determine output path
                string[] typeParts = resourceType.Split('.');
                int resourcesIndex = Array.IndexOf(typeParts, "Resources");

                // Validate resource type format
                if (resourcesIndex == -1 || resourcesIndex >= typeParts.Length - 1)
                {
                    Console.WriteLine($"Invalid resource type: {resourceType}");
                    continue;
                }

                // Extract subdirectories from namespace
                string[] subdirectories = typeParts
                    .Skip(resourcesIndex + 1)
                    .Take(typeParts.Length - resourcesIndex - 2)
                    .ToArray();

                string resourceName = typeParts.Last();
                string outputDir = Path.Combine(outputPath, projectDir, "Resources", Path.Combine(subdirectories));

                // Generate translations for each language
                foreach (var language in languages)
                {
                    string resxFileName = string.IsNullOrEmpty(language)
                        ? $"{resourceName}.resx"
                        : $"{resourceName}.{language}.resx";

                    string outputFilePath = Path.Combine(outputDir, resxFileName);
                    Directory.CreateDirectory(outputDir);

                    // Load existing file or create new structure
                    var resxXml = File.Exists(outputFilePath)
                        ? XElement.Load(outputFilePath)
                        : CreateResxXmlStructure();

                    // Track existing keys to prevent duplicates
                    var existingKeys = new HashSet<string>(
                        resxXml.Descendants("data")
                            .Select(e => e.Attribute("name")?.Value)
                            .Where(name => !string.IsNullOrEmpty(name))
                    );

                    // Add new translations
                    foreach (string key in keys)
                    {
                        if (existingKeys.Contains(key)) continue;

                        string value = FormatLocalizerKey(key);
                        string translation = "";

                        // Translate only for non-source languages
                        if (!string.IsNullOrEmpty(language) && !language.Equals("en") && !language.Equals(""))
                            translation = await Translate(value, language);

                        translation = string.IsNullOrEmpty(translation) ? value : translation;

                        resxXml.Add(new XElement("data",
                            new XAttribute("name", key),
                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                            new XElement("value", translation)));
                    }

                    resxXml.Save(outputFilePath);
                }
            }
        }
    }

    /// <summary>
    /// Creates the basic XML structure for .resx files with required headers and schema
    /// </summary>
    static XElement CreateResxXmlStructure()
    {
        var resxXml = new XElement("root");

        // Full schema from Visual Studio-generated .resx
        string xsdMarkup = @"<xsd:schema id='root' xmlns='' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
        <xsd:element name='root' msdata:IsDataSet='true'>
            <xsd:complexType>
                <xsd:choice maxOccurs='unbounded'>
                    <xsd:element name='data'>
                        <xsd:complexType>
                            <xsd:sequence>
                                <xsd:element name='value' type='xsd:string' minOccurs='0' msdata:Ordinal='1' />
                                <xsd:element name='comment' type='xsd:string' minOccurs='0' msdata:Ordinal='2' />
                            </xsd:sequence>
                            <xsd:attribute name='name' type='xsd:string' msdata:Ordinal='1' />
                            <xsd:attribute name='type' type='xsd:string' msdata:Ordinal='3' />
                            <xsd:attribute name='mimetype' type='xsd:string' msdata:Ordinal='4' />
                        </xsd:complexType>
                    </xsd:element>
                    <xsd:element name='resheader'>
                        <xsd:complexType>
                            <xsd:sequence>
                                <xsd:element name='value' type='xsd:string' minOccurs='0' msdata:Ordinal='1' />
                            </xsd:sequence>
                            <xsd:attribute name='name' type='xsd:string' use='required' />
                        </xsd:complexType>
                    </xsd:element>
                </xsd:choice>
            </xsd:complexType>
        </xsd:element>
    </xsd:schema>";

        resxXml.Add(XElement.Parse(xsdMarkup));

        // Add resheaders with exact VS values
        resxXml.Add(new XElement("resheader",
            new XAttribute("name", "resmimetype"),
            new XElement("value", "text/microsoft-resx")));

        resxXml.Add(new XElement("resheader",
            new XAttribute("name", "version"),
            new XElement("value", "1.3")));

        resxXml.Add(new XElement("resheader",
            new XAttribute("name", "reader"),
            new XElement("value",
                "System.Resources.ResXResourceReader, System.Windows.Forms, " +
                "Version=2.0.3500.0, Culture=neutral")));

        resxXml.Add(new XElement("resheader",
            new XAttribute("name", "writer"),
            new XElement("value",
                "System.Resources.ResXResourceWriter, System.Windows.Forms, " +
                "Version=2.0.3500.0, Culture=neutral")));

        return resxXml;
    }

    /// <summary>
    /// Formats camelCase/PascalCase strings into human-readable format
    /// Example: "FirstName" becomes "First name"
    /// </summary>
    public static string FormatLocalizerKey(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Any(Char.IsWhiteSpace))
        {
            return input;
        }

        StringBuilder formattedString = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char currentChar = input[i];
            if (char.IsUpper(currentChar) && i > 0)
            {
                formattedString.Append(" ");
                formattedString.Append(char.ToLower(currentChar));
            }
            else
            {
                formattedString.Append(currentChar);
            }
        }

        return formattedString.ToString();
    }

    /// <summary>
    /// Translates text using MyMemory Translation API
    /// </summary>
    private async static Task<string> Translate(string text, string targetLang)
    {
        try
        {
            string url = $"{baseUrl}get?q={WebUtility.UrlEncode(text)}&langpair=en|{targetLang}";
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            var translationResult = JsonConvert.DeserializeObject<TranslationResult>(responseJson);

            if (translationResult?.responseStatus == 200 && !string.IsNullOrEmpty(translationResult.TranslatedText))
            {
                Console.WriteLine($"Translated: {text} => {translationResult.TranslatedText}");
                return translationResult.TranslatedText;
            }

            Console.WriteLine($"Translation failed for: {text}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Translation error: {ex.Message}");
        }

        return text; // Fallback to original text
    }

    // Translation API response classes
    internal class TranslationResult
    {
        [JsonProperty("responseStatus")]
        public int responseStatus { get; set; }

        [JsonProperty("responseData")]
        public TranslationData ResponseData { get; set; }

        public string TranslatedText => ResponseData?.translatedText;
    }

    public class TranslationData
    {
        [JsonProperty("translatedText")]
        public string translatedText { get; set; }
    }
}