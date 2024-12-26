
# Localization File Generator for MVC Projects

This tool automates the creation of `.resx` localization files for MVC projects. It scans views (`.cshtml`) and controllers (`*Controller.cs`) for localization keys and generates `.resx` files for specified languages. The tool also supports translation through the MyMemory API.

---

## Features

- **Automated Key Extraction**: Scans `.cshtml` and `*Controller.cs` files for keys using patterns like `@Localizer["Key"]` and `_localizer["Key"]`.
- **Neutral and Language-Specific Files**: Creates both neutral (`.resx`) and language-specific (`.en.resx`, `.tr.resx`) files.
- **Translation Support**: Automatically translates keys into target languages using the MyMemory API.
- **MVC-Compatible**: Works seamlessly with ASP.NET MVC projects.

---

## Getting Started

### Prerequisites

- .NET SDK (6.0 or later)
- An MVC project with `@Localizer["Key"]` or `_localizer["Key"]` in views and controllers.

### Installation

Clone the repository and build the project using your preferred IDE or the CLI.

### Usage

1. Place your MVC project in a directory and locate its root path.
2. Run the tool and provide the following inputs:
    - **Project Path**: The root path of your MVC project.
    - **Output Path**: Directory where the generated `.resx` files will be saved.

### Example Input

1. Input Project Path: `D:\MVC\MySolution\MyProject`
2. Input Output Path: `D:\MVC\MySolution\MyProject\Resources`

### Supported Key Syntax

Keys should follow the patterns below:

- `@Localizer["WelcomeToMyPage"]`
- `_localizer["WelcomeToMyPage"]`

Replace `"WelcomeToMyPage"` with any desired key.

### Output

The tool will create `.resx` files for each language specified in the code. For example:

- `Views\Home\Index.resx` (Neutral)
- `Views\Home\Index.en.resx` (English)
- `Views\Home\Index.tr.resx` (Turkish)

---

## Translation API

The tool uses the MyMemory API to translate keys. If a translation is unavailable, it will use the original key.

### API Details

- Base URL: `https://api.mymemory.translated.net/`
- Example Request: `https://api.mymemory.translated.net/get?q=Hello&langpair=en|tr`

---

## File Structure

```
LocalizationFileGenerator
│
├── Program.cs
├── LocalizationFileGenerator.cs
└── README.md
```

---

## Contributing

Feel free to fork the repository, make improvements, and submit pull requests.

---

## Disclaimer

This tool is provided as-is and is not affiliated with MyMemory or any other organization.

---

Enjoy seamless localization for your MVC projects!
