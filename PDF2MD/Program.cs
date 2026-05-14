using Pdf2Md;
using Pdf2Md.Commands;
using Spectre.Console.Cli;
using System.Reflection;

var app = new CommandApp<ConvertCommand>();
var assembly = Assembly.GetExecutingAssembly();

app.Configure(config =>
{
    config.SetApplicationName("pdf2md");
    config.SetApplicationVersion(ApplicationVersion.GetDisplayVersion(assembly));

    config.AddExample(new[] { "document.pdf" });
    config.AddExample(new[] { "document.pdf", "--format", "md" });
    config.AddExample(new[] { "document.pdf", "--format", "md", "--split-by-title" });
    config.AddExample(new[] { "document.pdf", "--format", "html" });
    config.AddExample(new[] { "document.pdf", "--format", "html", "--split-html-by-title" });
    config.AddExample(new[] { "document.pdf", "--format", "both" });
    config.AddExample(new[] { "document.pdf", "--format", "both", "--split-by-title" });
    config.AddExample(new[] { "document.pdf", "-o", "output/result", "-f", "md", "-d", "output/images" });
});

return app.Run(args);
