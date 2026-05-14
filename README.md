# pdf2md

A **C# 10** command-line tool built with [Spectre.Console](https://spectreconsole.net/) that converts PDF files to **Markdown** (`.md`) and/or **HTML** (`.html`), with optional **image extraction**.

---

## Features

- 📄 Convert any PDF to **Markdown** or **HTML** (or both at once)
- 🖼️ **Extract embedded images** from PDFs and save them as `.png` / `.jpg` files
- 🔤 **Automatic heading detection** based on font size (H1, H2, H3)
- 📊 **Beautiful CLI output** with progress bars and results table (powered by Spectre.Console)
- ✅ Validates input and provides clear error messages

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

---

## Getting Started

### Build

```bash
dotnet build src/Pdf2Md/Pdf2Md.csproj -c Release
```

### Run

```bash
dotnet run --project src/Pdf2Md/Pdf2Md.csproj -- <input.pdf> [options]
```

Or publish a self-contained executable:

```bash
dotnet publish src/Pdf2Md/Pdf2Md.csproj -c Release -o ./out
./out/pdf2md <input.pdf> [options]
```

---

## Usage

```
USAGE:
    pdf2md <input> [OPTIONS]

ARGUMENTS:
    <input>    Path to the input PDF file

OPTIONS:
    -h, --help              Prints help information
    -v, --version           Prints version information
    -o, --output            Base output path (without extension)
                            Defaults to the input file name in the same directory
    -f, --format            Output format: md, html, or both  [default: md]
    -e, --extract-images    Extract images from the PDF
    -d, --images-dir        Directory to save extracted images
                            Defaults to <output>_images
```

### Examples

```bash
# Convert to Markdown (default)
pdf2md document.pdf

# Convert to HTML
pdf2md document.pdf --format html

# Convert to both Markdown and HTML
pdf2md document.pdf --format both

# Convert and extract images
pdf2md document.pdf --format both --extract-images

# Specify output path and custom image directory
pdf2md document.pdf -o output/result -f both -e -d output/images
```

---

## Output

Given `document.pdf`, the tool produces:

| Flag | Output |
|------|--------|
| `-f md` | `document.md` |
| `-f html` | `document.html` |
| `-f both` | `document.md` + `document.html` |
| `-e` | `document_images/page1_img1.png`, etc. |

Images are referenced as relative paths in both the `.md` and `.html` outputs, so opening the `.html` file in a browser will display the extracted images correctly.

---

## Project Structure

```
pdf2md.sln
src/
└── Pdf2Md/
    ├── Pdf2Md.csproj
    ├── Program.cs
    ├── Commands/
    │   ├── ConvertCommand.cs    # Main Spectre.Console command
    │   └── ConvertSettings.cs  # CLI options and validation
    ├── Converters/
    │   ├── PdfPageParser.cs     # Shared PDF text extraction logic
    │   ├── PdfToMarkdownConverter.cs
    │   └── PdfToHtmlConverter.cs
    ├── Extractors/
    │   └── ImageExtractor.cs   # PDF image extraction
    └── Models/
        └── PageContent.cs      # Shared data models
```

---

## Dependencies

| Package | Version | License |
|---------|---------|---------|
| [PdfPig](https://uglytoad.github.io/PdfPig/) | 0.1.14 | Apache 2.0 |
| [Spectre.Console.Cli](https://spectreconsole.net/) | 0.55.0 | MIT |

---

## License

MIT — see [LICENSE](LICENSE).
