using System;
using System.IO;
using Tesseract;
using Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace Infrastructure.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly string _tessDataPath;

        public TesseractOcrService()
        {
            // Use the application base directory (where the app runs)
            _tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

            Console.WriteLine($"=== TESSERACT INITIALIZATION ===");
            Console.WriteLine($"Looking for tessdata at: {_tessDataPath}");

            // Verify tessdata directory exists
            if (!Directory.Exists(_tessDataPath))
            {
                throw new DirectoryNotFoundException($"❌ tessdata directory not found at: {_tessDataPath}");
            }
            Console.WriteLine($"✅ Tessdata directory found");

            // Verify eng.traineddata exists
            var engDataPath = Path.Combine(_tessDataPath, "eng.traineddata");
            if (!File.Exists(engDataPath))

            {
                throw new FileNotFoundException($"❌ eng.traineddata not found at: {engDataPath}");
            }

            var fileInfo = new FileInfo(engDataPath);
            Console.WriteLine($" eng.traineddata found - Size: {fileInfo.Length} bytes");

            // Test Tesseract initialization
            try
            {
                using var testEngine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                Console.WriteLine($"✅ Tesseract engine initialized successfully!");
            }
            catch (Exception ex)
            {
                throw new Exception($"❌ Tesseract engine test failed: {ex.Message}", ex);
            }

            Console.WriteLine($"=== TESSERACT READY ===");
        }

        public string ExtractText(Stream imageStream)
        {
            string tempFilePath = null;
            try
            {
                Console.WriteLine("🔄 Starting OCR processing...");

                // Preprocess with ImageSharp
                using var image = Image.Load(imageStream);
                Console.WriteLine($"✅ Image loaded - Format: {image.Metadata.DecodedImageFormat?.Name}, Size: {image.Width}x{image.Height}");

                image.Mutate(x => x
                    .AutoOrient()
                    .Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(1600, 1600) })
                    .Grayscale()
                    .Contrast(1.1f)
                    .BinaryThreshold(0.5f)
                );

                Console.WriteLine("✅ Image preprocessing completed");

                // Create a temporary file with .png extension for Tesseract
                tempFilePath = Path.GetTempFileName();
                var finalTempPath = Path.ChangeExtension(tempFilePath, ".png");

                // Save as PNG explicitly
                image.Save(finalTempPath, new PngEncoder());
                Console.WriteLine($"✅ Temporary image saved: {finalTempPath}");

                // Use Tesseract to extract text
                using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(finalTempPath);
                using var page = engine.Process(img);

                var extractedText = page.GetText() ?? string.Empty;
                Console.WriteLine($"✅ OCR completed. Extracted {extractedText.Length} characters");

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    Console.WriteLine($"📝 Extracted text: {extractedText.Replace("\n", "\\n")}");
                }

                return extractedText;
            }
            catch (Exception ex)
            {
                // More detailed error information
                var errorDetails = $"OCR processing failed.\n" +
                                  $"Tessdata Path: {_tessDataPath}\n" +
                                  $"Engine Error: {ex.Message}\n" +
                                  $"Inner Exception: {ex.InnerException?.Message}";
                Console.WriteLine($"❌ {errorDetails}");
                throw new Exception(errorDetails, ex);
            }
            finally
            {
                // Clean up temporary files
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    Console.WriteLine("✅ Cleaned up .tmp file");
                }

                if (tempFilePath != null)
                {
                    var pngPath = Path.ChangeExtension(tempFilePath, ".png");
                    if (pngPath != null && File.Exists(pngPath))
                    {
                        File.Delete(pngPath);
                        Console.WriteLine("✅ Cleaned up .png file");
                    }
                }
            }
        }
    }
}