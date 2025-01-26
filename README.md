# Localization File Generator

Automated .resx file generator for .NET applications with translation capabilities

![.NET Version](https://img.shields.io/badge/.NET-6.0%2B-blue)

## Features

- 🎯 **MVC-Optimized**  
  Built for ASP.NET MVC projects with:
  ```text
  Controllers/      Views/       Models/
  ├─ *.cs          ├─ *.cshtml  └─ DataAnnotations
  ```
- 🔍 **Automatic Detection**  
  Scans controllers, views and model validations
  Scans `.cs` and `.cshtml` files for localization patterns
- 🌐 **Multi-language Support**  
  Generates resource files for en, tr, and custom languages
- 📂 **Structured Output and Preservation**
  Mirrors your MVC folder hierarchy in Resources/
  Creates `Resources` folders mirroring project structure
- 🔄 **Translation API Integration**  
  Uses MyMemory Translation service for automatic translations
- ⚠️ **Duplicate Prevention**  
  Ensures unique keys in resource files

## Requirements

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) or later
- Internet connection (for translation API)
- Valid project directory structure

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/localization-generator.git
   ```

2. Navigate to project directory:

   ```bash
   cd localization-generator
   ```

3. Build the solution:

   ```bash
   dotnet build
   ```

## Usage

### Ideal for MVC Projects
```text
Original:
/Controllers/HomeController.cs
/Views/Home/Index.cshtml

Generated:
/Resources/Controllers/HomeController.en.resx
/Resources/Views/Home/Index.tr.resx
```

1. Run the generator:

   ```bash
   dotnet run --project LocalizationFileGenerator.csproj
   ```

2. Provide paths when prompted:

   ```text
   Enter the project path:
   D:\Projects\YourSolution

   Enter the output path for .resx files: 
   D:\Projects\YourSolution
   ```

3. Generated files will appear in:

   ```text
   YourSolution/
   └── Resources/
       └── [Mirrored project structure]/
           ├── FileName.en.resx
           ├── FileName.tr.resx
           └── FileName.resx
   ```

## Configuration

### Adding Languages

Modify the `languages` array in code:

```csharp
string[] languages = { 
    "",        // Neutral culture
    "en",      // English
    "tr",      // Turkish
    "de"       // Add new languages
};
```

### Data Annotation Support

Format validation attributes:

```csharp
[Required(ErrorMessageResourceType = typeof(Resources.User),
          ErrorMessageResourceName = "NameRequired")]
public string Name { get; set; }
```

## Translation Process

### API Integration

Translation requests use:

```csharp
string url = $"{baseUrl}get?q={text}&langpair=en|{targetLang}";
```

### Response Handling

```csharp
if (translationResult?.responseStatus == 200)
{
    return translationResult.TranslatedText;
}
else
{
    return text; // Fallback to original
}
```

## Limitations

- ⏳ Translation API rate limits (1000/day free tier)
- 📁 Requires consistent project structure
- 🔠 First-letter capitalization not preserved

## License

MIT License - See [LICENSE](LICENSE) for full text

---

**Note:** Always test with a sample project before running on production code!
