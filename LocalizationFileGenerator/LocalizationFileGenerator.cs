using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

class LocalizationFileGenerator
{
    private const string baseUrl = "https://api.mymemory.translated.net/";
    private static HttpClient httpClient = new HttpClient();
    static async Task Main(string[] args)
    {
        
        // Step 1: Input paths for solution and output
        Console.WriteLine("Enter the project path:");
        string solutionPath = @"D:\MVC\SomeSolution\SomeProject"; 

        Console.WriteLine("Enter the output path for .resx files:");
        string outputPath = @"D:\MVC\SomeSolution\SomeProject";

        // Step 2: Define the languages to iterate
        string[] languages = { "","en", "tr" }; // Do not remove "" for it is used to create neutral values

        // Step 3: Find all files to scan for localizer keys
        var files = GetFilesToScan(solutionPath);

        // Step 4: Iterate through each file and process
        foreach (var file in files)
        {
            await ProcessFile(file, languages, solutionPath, outputPath);
        }
        
    }

    static List<string> GetFilesToScan(string solutionPath)
    {
        // Get all .cshtml and Controller.cs files in the solution folder, excluding those starting with "_"
        var files = new List<string>();

        // Get all .cshtml files excluding those starting with "_"
        var cshtmlFiles = Directory.GetFiles(solutionPath, "*.cshtml", SearchOption.AllDirectories)
                                    .Where(file => !Path.GetFileName(file).StartsWith("_"))
                                    .ToList();

        // Get all Controller.cs files excluding those starting with "_"
        var controllerFiles = Directory.GetFiles(solutionPath, "*Controller.cs", SearchOption.AllDirectories)
                                        .Where(file => !Path.GetFileName(file).StartsWith("_"))
                                        .ToList();

        files.AddRange(cshtmlFiles);
        files.AddRange(controllerFiles);

        return files;
    }

    static async Task ProcessFile(string file, string[] languages, string solutionPath, string outputPath)
    {
        // Read the content of the file
        string content = File.ReadAllText(file);

        // Regex pattern to find localizer keys in the file
        var localizerPattern = new Regex(@"(?:@Localizer\[""(.*?)""\]|_localizer\[""(.*?)""])", RegexOptions.IgnoreCase);

        var matches = localizerPattern.Matches(content);
        if (matches.Count == 0) return;

        // Get the relative path for the output file
        string relativePath = Path.GetRelativePath(solutionPath, file);
        string directoryPath = Path.GetDirectoryName(relativePath);

        foreach (var language in languages)
        {
            // Define the output file name
            string resxFileName;
            if (string.IsNullOrEmpty(language))
            {
                resxFileName = Path.GetFileNameWithoutExtension(file) + ".resx";
            }
            else
            {
                resxFileName = Path.GetFileNameWithoutExtension(file) + "." + language + ".resx";
            }
            Console.WriteLine($"Proccessing for language : {language} - File {resxFileName}");
            string outputFilePath = Path.Combine(outputPath, directoryPath, resxFileName);

            // Ensure the output directory exists
            string outputDirectory = Path.GetDirectoryName(outputFilePath);
            Directory.CreateDirectory(outputDirectory);

            // Create the XML structure for the resx file
            var resxXml = new XElement("root");

            // Add the XSD schema part (this is added before the resheader)
            string xsdMarkup =
                @"<xsd:schema id='root' xmlns='' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
                    <xsd:import namespace='http://www.w3.org/XML/1998/namespace'/>
                    <xsd:element name='root' msdata:IsDataSet='true'>
                        <xsd:complexType>
                            <xsd:choice maxOccurs='unbounded'>
                                <xsd:element name='metadata'>
                                    <xsd:complexType>
                                        <xsd:sequence>
                                            <xsd:element name='value' type='xsd:string' minOccurs='0'/>
                                        </xsd:sequence>
                                        <xsd:attribute name='name' use='required' type='xsd:string'/>
                                        <xsd:attribute name='type' type='xsd:string'/>
                                        <xsd:attribute name='mimetype' type='xsd:string'/>
                                        <xsd:attribute ref='xml:space'/>
                                    </xsd:complexType>
                                </xsd:element>
                                <xsd:element name='assembly'>
                                    <xsd:complexType>
                                        <xsd:attribute name='alias' type='xsd:string'/>
                                        <xsd:attribute name='name' type='xsd:string'/>
                                    </xsd:complexType>
                                </xsd:element>
                                <xsd:element name='data'>
                                    <xsd:complexType>
                                        <xsd:sequence>
                                            <xsd:element name='value' type='xsd:string' minOccurs='0' msdata:Ordinal='1'/>
                                            <xsd:element name='comment' type='xsd:string' minOccurs='0' msdata:Ordinal='2'/>
                                        </xsd:sequence>
                                        <xsd:attribute name='name' type='xsd:string' use='required' msdata:Ordinal='1'/>
                                        <xsd:attribute name='type' type='xsd:string' msdata:Ordinal='3'/>
                                        <xsd:attribute name='mimetype' type='xsd:string' msdata:Ordinal='4'/>
                                        <xsd:attribute ref='xml:space'/>
                                    </xsd:complexType>
                                </xsd:element>
                                <xsd:element name='resheader'>
                                    <xsd:complexType>
                                        <xsd:sequence>
                                            <xsd:element name='value' type='xsd:string' minOccurs='0' msdata:Ordinal='1'/>
                                        </xsd:sequence>
                                        <xsd:attribute name='name' type='xsd:string' use='required'/>
                                    </xsd:complexType>
                                </xsd:element>
                            </xsd:choice>
                        </xsd:complexType>
                    </xsd:element>
                </xsd:schema>";

            // Load the XSD markup into the XML structure using XmlReader
            XmlSchemaSet schemas = new XmlSchemaSet();
            using (XmlReader reader = XmlReader.Create(new StringReader(xsdMarkup)))
            {
                schemas.Add("", reader);
            }

            // Add the schema to the XML structure
            XElement schemaElement = XElement.Parse(xsdMarkup);
            resxXml.Add(schemaElement);

            // Add the header information
            resxXml.Add(new XElement("resheader", new XAttribute("name", "resmimetype"),
                new XElement("value", "text/microsoft-resx")));
            resxXml.Add(new XElement("resheader", new XAttribute("name", "version"),
                new XElement("value", "2.0")));
            resxXml.Add(new XElement("resheader", new XAttribute("name", "reader"),
                new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral")));
            resxXml.Add(new XElement("resheader", new XAttribute("name", "writer"),
                new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral")));

            // Add each found localizer as data elements
            foreach (Match match in matches)
            {
                string key;
                if (string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    key = match.Groups[2].Value;
                }
                else
                {
                    key = match.Groups[1].Value;
                }
                string value = FormatLocalizerKey(key);
                string translation ="";
                if (!string.IsNullOrEmpty(value) && !language.Equals("") && !language.Equals("en"))
                {
                    translation = await Translate(value, language); // In here I want to wait for the translation to be completed before moving on to the next key
                }
                if (string.IsNullOrEmpty(translation))
                {
                    // Fallback to the original formatted key if translation fails
                    translation = value;
                }
                // Create the data element with xml: space = "preserve"
                XElement dataElement = new XElement("data",
                    new XAttribute("name", key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"), // Add xml:space="preserve"
                    new XElement("value", translation));

                resxXml.Add(dataElement);
            }

            // Save the XML to the output file
            resxXml.Save(outputFilePath);
        }
    }

    public static string FormatLocalizerKey(string input)
    {
        // Ensure the input is not null or empty
        if (string.IsNullOrEmpty(input) || input.Any(Char.IsWhiteSpace))
        {
            return input;
        }

        // Use StringBuilder for efficient string manipulation
        StringBuilder formattedString = new StringBuilder();

        // Loop through each character in the string
        for (int i = 0; i < input.Length; i++)
        {
            char currentChar = input[i];

            // If it's a capital letter and not the first letter
            if (char.IsUpper(currentChar) && i > 0)
            {
                // Add a space and the lowercase version of the current letter
                formattedString.Append(" ");
                formattedString.Append(char.ToLower(currentChar));
            }
            else
            {
                // Otherwise, just add the character as it is
                formattedString.Append(currentChar);
            }
        }

        return formattedString.ToString();
    }
    private async static Task<string> Translate(string text, string targetLang)
    {
        try
        {
            string url = $"{baseUrl}get?q={WebUtility.UrlEncode(text)}&langpair=en|{targetLang}";
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
            httpResponseMessage.EnsureSuccessStatusCode();

            string responseJson = await httpResponseMessage.Content.ReadAsStringAsync();
            var translationResult = JsonConvert.DeserializeObject<TranslationResult>(responseJson);

            if (translationResult?.responseStatus == 200 && !string.IsNullOrEmpty(translationResult.TranslatedText))
            {
                Console.WriteLine($"Text: {text} is translated to {translationResult.TranslatedText}");
                return translationResult.TranslatedText;
            }
            // Log or handle cases where the translation is invalid
            Console.WriteLine($"Translation failed for: {text}");
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error during translation: {ex.Message}");
        }

        // Return the original text as a fallback
        return text;
    }

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

