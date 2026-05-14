using Pdf2Md.Commands;
using Spectre.Console.Cli;

var app = new CommandApp<ConvertCommand>();

app.Configure(config =>
{
    config.SetApplicationName("pdf2md");
    config.SetApplicationVersion("1.0.0");

    config.AddExample(new[] { "document.pdf" });
    config.AddExample(new[] { "document.pdf", "--format", "html" });
    config.AddExample(new[] { "document.pdf", "--format", "both" });
    config.AddExample(new[] { "document.pdf", "-o", "output/result", "-f", "html", "-d", "output/images" });
});

return app.Run(args);
