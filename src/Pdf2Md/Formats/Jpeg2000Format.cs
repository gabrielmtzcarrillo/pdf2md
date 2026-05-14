using FileSignatures.Formats;

namespace Pdf2Md.Formats;

internal sealed class Jpeg2000Format : Image
{
    private static readonly byte[] MagicBytes =
    {
        0x00, 0x00, 0x00, 0x0C,
        0x6A, 0x50, 0x20, 0x20,
        0x0D, 0x0A, 0x87, 0x0A
    };

    public Jpeg2000Format()
        : base(MagicBytes, "image/jp2", "jp2")
    {
    }
}
