
using System.IO;
using SkiaSharp;

namespace AssetManager.Infrastructure.DOC;

public class ImageProcessor
{
    public static void CombineImages(string cardImagePath, string frameImagePath, string outputPath)
    {
        using var cardImage = SKBitmap.Decode(cardImagePath);
        using var frameImage = SKBitmap.Decode(frameImagePath);

        // Ensure both images are the same size
        int width = Math.Max(cardImage.Width, frameImage.Width);
        int height = Math.Max(cardImage.Height, frameImage.Height);

        using var combinedBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(combinedBitmap);

        // Draw the card image first
        canvas.DrawBitmap(cardImage, new SKPoint(0, 0));

        // Draw the ornate frame on top
        canvas.DrawBitmap(frameImage, new SKPoint(0, 0));

        // Save final image
        using var image = SKImage.FromBitmap(combinedBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(outputPath, data.ToArray());
    }
}
