using Pdf2Md.Converters;

namespace PDF2MD.Tests;

public class ImageReferencePathTests
{
    [Fact]
    public void GetReferenceRoot_ProducesDotPrefixedSiblingPath()
    {
        var referenceRoot = ImageReferencePath.GetReferenceRoot(
            @"D:\output\images",
            @"D:\output\document.md");

        var imagePath = ImageReferencePath.GetImagePath(
            "page1_img1.png",
            @"D:\output\images",
            referenceRoot);

        Assert.Equal("./images/page1_img1.png", imagePath);
    }

    [Fact]
    public void GetReferenceRoot_ProducesParentRelativePathForNestedOutputs()
    {
        var referenceRoot = ImageReferencePath.GetReferenceRoot(
            @"D:\output\shared\images",
            @"D:\output\parts\chapter1\document.html");

        var imagePath = ImageReferencePath.GetImagePath(
            "page2_img3.png",
            @"D:\output\shared\images",
            referenceRoot);

        Assert.Equal("../../shared/images/page2_img3.png", imagePath);
    }
}
