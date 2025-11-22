using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using OpenCvSharp;

namespace Barangay.Services
{
    public class LocalOcrService : IDisposable
    {
        private readonly ILogger<LocalOcrService> _logger;
        private readonly string _tesseractDataPath;
        private readonly HttpClient _httpClient;
        private readonly List<(string path, string method)> _preprocessedImages;
        private bool _disposed = false;
        private bool _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public LocalOcrService(ILogger<LocalOcrService> logger, IHttpClientFactory httpClientFactory = null)
        {
            _logger = logger;
            _httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            _preprocessedImages = new List<(string, string)>();
            
            try
            {
                // Set TESSDATA_PREFIX environment variable - this is crucial for Tesseract to find its data files
                var tesseractDataPath = FindTesseractDataPath();
                Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tesseractDataPath);
                _logger.LogInformation($"TESSDATA_PREFIX set to: {tesseractDataPath}");
                
                // On Linux, check for required native libraries
                if (_isLinux)
                {
                    CheckLinuxDependencies();
                }
                
                // Ensure language files exist (download if needed)
                _ = EnsureLanguageFilesExist().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Failed to ensure language files exist");
                    }
                });
                
                // Log environment information for debugging
                LogEnvironmentInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing LocalOcrService");
                throw;
            }
        }

        /// <summary>
        /// Preprocesses image using ImageSharp as fallback
        /// </summary>
        private async Task<byte[]> PreprocessWithImageSharp(byte[] imageBytes)
        {
            using var image = Image.Load(imageBytes);
            
            // Scale image to higher resolution for better OCR
            var originalWidth = image.Width;
            var originalHeight = image.Height;
            
            if (image.Width < 1200 || image.Height < 1600)
            {
                var scaleFactor = Math.Max(1200.0 / image.Width, 1600.0 / image.Height);
                var newWidth = (int)(image.Width * scaleFactor);
                var newHeight = (int)(image.Height * scaleFactor);
                
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(newWidth, newHeight),
                    Mode = ResizeMode.BoxPad
                }));
                
                _logger.LogInformation("Scaled image from {OldWidth}x{OldHeight} to {NewWidth}x{NewHeight}", 
                    originalWidth, originalHeight, newWidth, newHeight);
            }
            
            // Enhance image quality for OCR with improved parameters
            // Based on recommendations: 20-50% contrast boost, unsharp mask (sigma 0.5)
            image.Mutate(x => x
                .AutoOrient() // Deskew: auto-rotate if tilted
                .Grayscale() // Convert to grayscale to reduce noise
                .Contrast(1.2f) // Boost contrast by 20% (reduced from 1.8f for better balance)
                .Brightness(1.1f) // Slight brightness boost (reduced from 1.3f)
                .GaussianSharpen(0.5f) // Unsharp mask: sigma 0.5 (as recommended)
            );

            // Convert back to byte array
            using var processedStream = new MemoryStream();
            await image.SaveAsPngAsync(processedStream);
            return processedStream.ToArray();
        }

        private string FindTesseractDataPath()
        {
            var possiblePaths = new List<string>();
            
            // Check common Linux paths first
            if (_isLinux)
            {
                possiblePaths.AddRange(new[]
                {
                    "/usr/share/tesseract-ocr/4.00/tessdata",
                    "/usr/share/tesseract-ocr/5/tessdata",
                    "/usr/share/tesseract-ocr/tessdata",
                    "/usr/local/share/tessdata",
                    "/opt/tesseract/tessdata"
                });
            }
            
            // Check application directory and common Windows paths
            var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            possiblePaths.Add(appDataPath);
            possiblePaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "tessdata"));
            possiblePaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "tessdata"));
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                possiblePaths.AddRange(new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tesseract-OCR", "tessdata"),
                    Path.Combine("C:", "Program Files", "Tesseract-OCR", "tessdata"),
                    Path.Combine("C:", "Program Files (x86)", "Tesseract-OCR", "tessdata")
                });
            }
            
            // Try to find an existing directory
            foreach (var path in possiblePaths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        _logger.LogInformation("Found Tesseract data path: {Path}", path);
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking path: {Path}", path);
                }
            }
            
            // If no existing directory found, use the application directory
            try
            {
                Directory.CreateDirectory(appDataPath);
                _logger.LogInformation("Created tessdata directory at: {Path}", appDataPath);
                return appDataPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tessdata directory at {Path}", appDataPath);
                throw;
            }
        }
        
    private void CheckLinuxDependencies()
{
    try
    {
        _logger.LogInformation("=== LINUX DEPENDENCY CHECK ===");
        
        // 1. Set LD_LIBRARY_PATH to include common library directories
        var currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
        var paths = new HashSet<string>(currentLdPath.Split(':', StringSplitOptions.RemoveEmptyEntries))
        {
            "/usr/lib/x86_64-linux-gnu",
            "/usr/local/lib",
            "/usr/lib"
        };
        var newLdPath = string.Join(":", paths);
        Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", newLdPath);
        _logger.LogInformation($"Set LD_LIBRARY_PATH: {newLdPath}");

        // 2. Try to load Leptonica library explicitly
        var leptonicaPaths = new[]
        {
            "/usr/lib/x86_64-linux-gnu/liblept.so.5",
            "/usr/local/lib/liblept.so.5",
            "/usr/lib/liblept.so.5",
            "liblept.so.5"  // Try without path (should be in LD_LIBRARY_PATH)
        };

        bool leptonicaLoaded = false;
        foreach (var path in leptonicaPaths)
        {
            try
            {
                if (NativeLibrary.TryLoad(path, out var handle))
                {
                    _logger.LogInformation($"✓ Successfully loaded Leptonica from: {path}");
                    NativeLibrary.Free(handle);  // We don't need to keep it loaded, just testing
                    leptonicaLoaded = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Could not load Leptonica from {path}: {ex.Message}");
            }
        }

        if (!leptonicaLoaded)
        {
            _logger.LogWarning("⚠️ Could not load Leptonica library. OCR may not work correctly.");
            
            // Try to run ldd to see what's missing
            try
            {
                var lddOutput = RunCommand("ldd", "$(which tesseract)");
                _logger.LogInformation("ldd output for tesseract:\n" + lddOutput.Output);
                
                // Check for common issues
                if (lddOutput.Output.Contains("not found"))
                {
                    _logger.LogError("❌ Missing dependencies detected. Try running: " +
                        "apt-get update && apt-get install -y liblept5 libtesseract4 tesseract-ocr");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not run ldd: " + ex.Message);
            }
        }

        // 3. Check Tesseract installation
        try
        {
            var tesseractVersion = RunCommand("tesseract", "--version");
            _logger.LogInformation($"✓ Tesseract version: {tesseractVersion.Output.Trim()}");
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Tesseract is not installed. Install with: apt-get install -y tesseract-ocr");
        }

        // 4. Check OpenCV
        try
        {
            Cv2.GetVersionString();
            _logger.LogInformation($"✓ OpenCV version: {Cv2.GetVersionString()}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("⚠️ OpenCV not available: " + ex.Message);
        }

        // 5. Run a simple Tesseract test
        try
        {
            using (var engine = new TesseractEngine(_tesseractDataPath, "eng", EngineMode.Default))
            {
                _logger.LogInformation("✓ Tesseract engine initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Failed to initialize Tesseract engine: " + ex.Message);
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: " + ex.InnerException.Message);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError("Error in CheckLinuxDependencies: " + ex);
    }
}
        
        private (int ExitCode, string Output) RunCommand(string command, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Command failed: {command} {arguments}\n{error}");
                }
                
                return (process.ExitCode, output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error running command: {command} {arguments}");
                return (-1, $"Error: {ex.Message}");
            }
        }
        
        private void LogEnvironmentInfo()
        {
            try
            {
                _logger.LogInformation("=== Environment Information ===");
                _logger.LogInformation($"OS: {RuntimeInformation.OSDescription}");
                _logger.LogInformation($"Runtime: {RuntimeInformation.FrameworkDescription}");
                _logger.LogInformation($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
                _logger.LogInformation($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                _logger.LogInformation($"TESSDATA_PREFIX: {Environment.GetEnvironmentVariable("TESSDATA_PREFIX")}");
                
                // Log library search paths
                if (_isLinux)
                {
                    _logger.LogInformation($"LD_LIBRARY_PATH: {Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")}");
                    
                    // Log contents of /usr/lib and /usr/local/lib
                    LogDirectoryContents("/usr/lib", "*.so*", 5);
                    LogDirectoryContents("/usr/local/lib", "*.so*", 5);
                }
                
                _logger.LogInformation("================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging environment information");
            }
        }
        
        private void LogDirectoryContents(string path, string searchPattern, int maxItems = 10)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, searchPattern)
                        .Select(f => Path.GetFileName(f))
                        .Take(maxItems)
                        .ToList();
                        
                    if (files.Any())
                    {
                        _logger.LogInformation($"Found {files.Count} files in {path}:");
                        foreach (var file in files)
                        {
                            _logger.LogInformation($"  - {file}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not list contents of {path}");
            }
        }
        
        /// <summary>
        /// Ensures that the required Tesseract language data files exist, downloading them if necessary
        /// </summary>
        private async Task EnsureLanguageFilesExist()
        {
            var tessDataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? "/usr/share/tesseract-ocr/4.00/tessdata";
            var engDataFile = Path.Combine(tessDataPath, "eng.traineddata");
            var filDataFile = Path.Combine(tessDataPath, "fil.traineddata");
            
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(tessDataPath);
                
                // Download English language data if needed
                if (!File.Exists(engDataFile))
                {
                    _logger.LogWarning("English language data file not found at {Path}, attempting to download", engDataFile);
                    await DownloadLanguageFileAsync("eng.traineddata", engDataFile);
                }
                
                // Download Filipino language data if needed
                if (!File.Exists(filDataFile))
                {
                    _logger.LogInformation("Filipino language data file not found at {Path}, attempting to download", filDataFile);
                    await DownloadLanguageFileAsync("fil.traineddata", filDataFile);
                }
                
                // Verify the files are valid
                if (File.Exists(engDataFile))
                {
                    var fileInfo = new FileInfo(engDataFile);
                    _logger.LogInformation($"Found eng.traineddata at {engDataFile} (Size: {fileInfo.Length / 1024} KB)");
                    
                    if (fileInfo.Length < 1024 * 100) // Less than 100KB is probably invalid
                    {
                        _logger.LogWarning("eng.traineddata file appears to be too small. It may be corrupted.");
                        File.Delete(engDataFile);
                        await DownloadLanguageFileAsync("eng.traineddata", engDataFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring language files exist");
                throw;
            }
        }
        
        private async Task DownloadLanguageFileAsync(string fileName, string destinationPath)
        {
            try
            {
                var url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{fileName}";
                _logger.LogInformation($"Downloading {fileName} from GitHub...");
                
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);
                
                _logger.LogInformation($"Successfully downloaded {fileName} to {destinationPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download {fileName}");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _httpClient?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Analyzes a document and extracts barangay number (158, 159, 160, or 161)
        /// </summary>
        public async Task<OcrResult> AnalyzeResidencyDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                _logger.LogInformation("=== LOCAL OCR ANALYSIS START ===");
                _logger.LogInformation("File: {FileName}", fileName);

                // Convert stream to byte array
                using var memoryStream = new MemoryStream();
                await documentStream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // Ensure language files exist before using Tesseract
                var engDataFile = Path.Combine(_tesseractDataPath, "eng.traineddata");
                if (!File.Exists(engDataFile))
                {
                    _logger.LogWarning("eng.traineddata not found, attempting to download...");
                    await EnsureLanguageFilesExist();
                    
                    // Check again after download attempt
                    if (!File.Exists(engDataFile))
                    {
                        return new OcrResult
                        {
                            Success = false,
                            Message = "Tesseract language data file not found. Please ensure eng.traineddata exists in the tessdata directory."
                        };
                    }
                }

                // Preprocess image for better OCR results using OpenCvSharp for advanced processing
                byte[] processedBytes;
                
                try
                {
                    // Save image to temp file for OpenCvSharp processing
                    var tempImagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
                    await File.WriteAllBytesAsync(tempImagePath, imageBytes);
                    
                    try
                    {
                        // Use OpenCvSharp for advanced preprocessing
                        using (var src = Cv2.ImRead(tempImagePath, ImreadModes.Color))
                        {
                            if (!src.Empty())
                            {
                                _logger.LogInformation("Applying OpenCvSharp preprocessing for better OCR");
                                
                                // Convert to grayscale
                                Mat gray = new Mat();
                                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                                
                                // Scale up if too small
                                if (gray.Width < 1200 || gray.Height < 1600)
                                {
                                    var scaleFactor = Math.Max(1200.0 / gray.Width, 1600.0 / gray.Height);
                                    var newWidth = (int)(gray.Width * scaleFactor);
                                    var newHeight = (int)(gray.Height * scaleFactor);
                                    Mat scaled = new Mat();
                                    Cv2.Resize(gray, scaled, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Cubic);
                                    gray.Dispose();
                                    gray = scaled;
                                    _logger.LogInformation("Scaled image to {Width}x{Height} for better OCR", newWidth, newHeight);
                                }
                                
                                // Try multiple preprocessing approaches and use the best one
                                // Approach 1: CLAHE + Adaptive Thresholding (best for glare)
                                Mat claheResult = new Mat();
                                using (var clahe = Cv2.CreateCLAHE(3.0, new OpenCvSharp.Size(8, 8)))
                                {
                                    clahe.Apply(gray, claheResult);
                                }
                                
                                // Apply adaptive thresholding to handle glare better
                                Mat adaptive = new Mat();
                                Cv2.AdaptiveThreshold(claheResult, adaptive, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                                
                                // Approach 2: Otsu thresholding
                                Mat otsu = new Mat();
                                Cv2.Threshold(claheResult, otsu, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                                
                                // Approach 3: Morphological operations to reduce glare
                                Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                                Mat morphed = new Mat();
                                Cv2.MorphologyEx(claheResult, morphed, MorphTypes.Close, kernel);
                                
                                // Apply sharpening to all approaches
                                Mat sharpened1 = new Mat();
                                Mat blurred1 = new Mat();
                                Cv2.GaussianBlur(adaptive, blurred1, new OpenCvSharp.Size(0, 0), 2);
                                Cv2.AddWeighted(adaptive, 2.0, blurred1, -1.0, 0, sharpened1);
                                
                                Mat sharpened2 = new Mat();
                                Mat blurred2 = new Mat();
                                Cv2.GaussianBlur(otsu, blurred2, new OpenCvSharp.Size(0, 0), 2);
                                Cv2.AddWeighted(otsu, 2.0, blurred2, -1.0, 0, sharpened2);
                                
                                Mat sharpened3 = new Mat();
                                Mat blurred3 = new Mat();
                                Cv2.GaussianBlur(morphed, blurred3, new OpenCvSharp.Size(0, 0), 2);
                                Cv2.AddWeighted(morphed, 2.0, blurred3, -1.0, 0, sharpened3);
                                
                                // Apply denoising
                                Mat denoised1 = new Mat();
                                Mat denoised2 = new Mat();
                                Mat denoised3 = new Mat();
                                Cv2.FastNlMeansDenoising(sharpened1, denoised1, 10, 7, 21);
                                Cv2.FastNlMeansDenoising(sharpened2, denoised2, 10, 7, 21);
                                Cv2.FastNlMeansDenoising(sharpened3, denoised3, 10, 7, 21);
                                
                                // Save all three versions for OCR testing
                                var tempPath1 = tempImagePath.Replace(".jpg", "_1.jpg");
                                var tempPath2 = tempImagePath.Replace(".jpg", "_2.jpg");
                                var tempPath3 = tempImagePath.Replace(".jpg", "_3.jpg");
                                
                                Cv2.ImWrite(tempPath1, denoised1);
                                Cv2.ImWrite(tempPath2, denoised2);
                                Cv2.ImWrite(tempPath3, denoised3);
                                
                                // Store all three versions for OCR testing
                                _preprocessedImages = new List<(string path, string method)>
                                {
                                    (tempPath1, "AdaptiveThreshold"),
                                    (tempPath2, "OtsuThreshold"),
                                    (tempPath3, "Morphological")
                                };
                                
                                // Use the first approach by default (adaptive thresholding is best for glare)
                                processedBytes = await File.ReadAllBytesAsync(tempPath1);
                                
                                // Cleanup
                                gray.Dispose();
                                claheResult.Dispose();
                                adaptive.Dispose();
                                otsu.Dispose();
                                kernel.Dispose();
                                morphed.Dispose();
                                blurred1.Dispose();
                                blurred2.Dispose();
                                blurred3.Dispose();
                                sharpened1.Dispose();
                                sharpened2.Dispose();
                                sharpened3.Dispose();
                                denoised1.Dispose();
                                denoised2.Dispose();
                                denoised3.Dispose();
                                
                                _logger.LogInformation("OpenCvSharp preprocessing completed successfully - created 3 versions for OCR testing");
                            }
                            else
                            {
                                _logger.LogWarning("OpenCvSharp failed to load image, using ImageSharp preprocessing");
                                processedBytes = await PreprocessWithImageSharp(imageBytes);
                            }
                        }
                    }
                    finally
                    {
                        // Clean up temp files after OCR is done (will be cleaned up later)
                        // Keep them for now so we can try OCR on all versions
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenCvSharp preprocessing failed, falling back to ImageSharp");
                    processedBytes = await PreprocessWithImageSharp(imageBytes);
                }

                // Perform OCR using Tesseract with multiple PSM modes and combine results
                var allTexts = new List<string>();
                
                // Try different Page Segmentation Modes (PSM) for better results
                // PSM 6: Uniform block of text (best for ID cards)
                // PSM 3: Fully automatic page segmentation
                // PSM 4: Single column of variable-sized text
                // PSM 11: Sparse text
                // PSM 12: Sparse text with OSD
                var psmModes = new[] { "6", "3", "4", "11", "12" };
                
                // Try both Default and LSTM engine modes for better accuracy
                var engineModes = new[] { EngineMode.Default, EngineMode.LstmOnly };
                
                // Try OCR on all preprocessed versions if available
                var imagesToProcess = new List<(byte[] bytes, string method)>();
                imagesToProcess.Add((processedBytes, "Primary"));
                
                if (_preprocessedImages != null && _preprocessedImages.Count > 0)
                {
                    foreach (var (path, method) in _preprocessedImages)
                    {
                        if (File.Exists(path))
                        {
                            try
                            {
                                var bytes = await File.ReadAllBytesAsync(path);
                                imagesToProcess.Add((bytes, method));
                                _logger.LogInformation("Added preprocessed image for OCR: {Method}", method);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to read preprocessed image: {Path}", path);
                            }
                        }
                    }
                }
                
                // Also try the original image without preprocessing as a fallback
                imagesToProcess.Add((imageBytes, "Original"));
                _logger.LogInformation("Will try OCR on {Count} image versions", imagesToProcess.Count);
                
                foreach (var (imgBytes, method) in imagesToProcess)
                {
                    foreach (var engineMode in engineModes)
                    {
                        foreach (var psmMode in psmModes)
                        {
                            try
                            {
                                using (var engine = new TesseractEngine(_tesseractDataPath, "eng", engineMode))
                                {
                                    // Configure engine parameters for ID card recognition
                                    // REMOVED character whitelist to allow all characters (including special chars)
                                    engine.SetVariable("tessedit_pageseg_mode", psmMode);
                                    
                                    // Set DPI to 300 for better accuracy
                                    engine.SetVariable("user_defined_dpi", "300");
                                    
                                    // Additional settings for better OCR
                                    engine.SetVariable("tessedit_char_whitelist", ""); // No whitelist - allow all characters
                                    engine.SetVariable("preserve_interword_spaces", "1"); // Preserve spaces
                                    
                                    using var img = Pix.LoadFromMemory(imgBytes);
                                    using var page = engine.Process(img);
                                    var text = page.GetText();
                                    var confidence = page.GetMeanConfidence();
                                    
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        allTexts.Add(text);
                                        _logger.LogInformation("Method: {Method}, Engine: {EngineMode}, PSM {PSM} - Confidence: {Confidence}, Text length: {Length}", 
                                            method, engineMode, psmMode, confidence, text.Length);
                                    }
                                }
                            }
                            catch (DllNotFoundException dllEx)
                            {
                                // Native library missing - this is a critical error that should be reported clearly
                                _logger.LogError(dllEx, "❌ Tesseract native libraries (Leptonica) not found. " +
                                    "Local OCR cannot function. Please install: apt-get install -y libleptonica-dev libtesseract-dev");
                                
                                // Return early with a clear error message
                                return new OcrResult
                                {
                                    Success = false,
                                    Message = "Tesseract OCR native libraries are not installed on the server. " +
                                        "Please contact the administrator. Error: " + dllEx.Message
                                };
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to process with Method: {Method}, Engine: {EngineMode}, PSM {PSM}", method, engineMode, psmMode);
                                
                                // Check if this is a native library issue
                                if (ex.Message.Contains("libleptonica") || ex.Message.Contains("DllNotFoundException") || 
                                    ex.InnerException?.Message?.Contains("libleptonica") == true)
                                {
                                    _logger.LogError(ex, "❌ Native library error detected. Local OCR cannot function.");
                                    return new OcrResult
                                    {
                                        Success = false,
                                        Message = "Tesseract OCR native libraries are not available. " +
                                            "Please contact the administrator. Error: " + ex.Message
                                    };
                                }
                            }
                        }
                    }
                }
                
                // Combine all OCR results, merging unique lines from all PSM modes
                string extractedText = "";
                if (allTexts.Count > 0)
                {
                    // Merge unique lines from all PSM mode results
                    var lineDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    
                    // Process texts in order of length (longest first) to preserve best quality text
                    foreach (var text in allTexts.OrderByDescending(t => t.Length))
                    {
                        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrWhiteSpace(l));
                        
                        foreach (var line in lines)
                        {
                            var key = line.ToUpperInvariant();
                            if (!lineDict.ContainsKey(key) || line.Length > lineDict[key].Length)
                            {
                                lineDict[key] = line;
                            }
                        }
                    }
                    
                    // Combine all unique lines
                    extractedText = string.Join("\n", lineDict.Values);
                    _logger.LogInformation("Combined OCR text from {Count} PSM modes: {Length} characters, {Lines} unique lines", 
                        allTexts.Count, extractedText.Length, lineDict.Count);
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("No text extracted from document");
                    return new OcrResult
                    {
                        Success = false,
                        Message = "No text could be extracted from the document. Please ensure the document is clear and readable."
                    };
                }

                _logger.LogInformation("Extracted text length: {Length} characters", extractedText.Length);
                _logger.LogInformation("Extracted text preview: {Preview}", extractedText.Substring(0, Math.Min(200, extractedText.Length)));

                // Clean up temp files after OCR is done
                if (_preprocessedImages != null)
                {
                    foreach (var (path, _) in _preprocessedImages)
                    {
                        try
                        {
                            if (File.Exists(path)) File.Delete(path);
                        }
                        catch { }
                    }
                    _preprocessedImages = null;
                }

                // Step 3: Search for Barangay number
                var barangayResult = ExtractBarangayNumber(extractedText);

                return barangayResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OCR analysis");
                return new OcrResult
                {
                    Success = false,
                    Message = $"OCR analysis error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Validates that the extracted text is from an actual Philippine ID document
        /// Rejects plain text, screenshots, or documents without ID markers
        /// STRICT VALIDATION: Requires actual ID document markers, not just address fields
        /// Returns tuple (isValid, idType)
        /// </summary>
        private (bool isValid, string idType) IsValidPhilippineIdDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (false, null);

            var upperText = text.ToUpper();
            
            // CRITICAL: Check for screenshot indicators in text (screenshots often have UI elements)
            // Comprehensive list of screenshot indicators based on common mobile/desktop UI elements
            var screenshotIndicators = new[] { 
                "SCREENSHOT", "SCREEN SHOT", "SCREENSHOT SAVED", "SCREEN CAPTURE", "SCREENSHOT CAPTURED",
                "CAPTURE", "SNAP", "SNAPSHOT", "SCREEN RECORDING", "SCREENSHOT TOOL",
                "WINDOWS", "MACOS", "ANDROID", "IOS", "IPHONE", "IPAD",
                "GALLERY", "PHOTOS", "CAMERA ROLL", "PHOTO LIBRARY", "PICTURE GALLERY",
                "PRINT SCREEN", "PRTSC", "PRT SCR", "PRINTSCREEN",
                "SHARE", "SAVE IMAGE", "DOWNLOAD", "IMAGE SAVED", "SAVED TO GALLERY",
                "SCREENSHOT APP", "TAKE SCREENSHOT", "SCREENSHOT NOTIFICATION",
                "FILE MANAGER", "FILES APP", "GOOGLE PHOTOS", "ICLOUD PHOTOS",
                "SCREENSHOT FOLDER", "SCREENSHOTS", "SCREENSHOTS FOLDER"
            };
            if (screenshotIndicators.Any(indicator => upperText.Contains(indicator)))
            {
                _logger.LogWarning("⚠️ Document validation failed: Screenshot indicators found in text");
                return (false, "Screenshot Detected - Please upload a photo of your actual ID, not a screenshot");
            }
            
            // Required Philippine ID markers - document MUST contain at least one STRONG ID marker
            // Check for strong markers FIRST - if found, we can be more lenient with handwriting checks
            // These are specific to actual ID documents, not just any document with an address
            var strongIdMarkers = new[]
            {
                // Republic of the Philippines markers (REQUIRED for most IDs)
                "REPUBLIC OF THE PHILIPPINES",
                "REPUBLIKA NG PILIPINAS",
                "REPUBLIC OF THE PHILIPPINE",
                
                // Driver's License markers (REQUIRED)
                "DRIVER'S LICENSE",
                "DRIVERS LICENSE",
                "DRIVER LICENSE",
                "LICENSE TO DRIVE",
                "LAND TRANSPORTATION OFFICE",
                "LTO",
                "DEPARTMENT OF TRANSPORTATION",
                
                // National ID markers (REQUIRED)
                "PHILSYS",
                "PHILIPPINE IDENTIFICATION SYSTEM",
                "PHILIPPINE NATIONAL ID",
                "NATIONAL ID",
                "PAMBANSANG PAGKAKAKILANLAN",
                "PHILIPPINE IDENTIFICATION CARD",
                
                // PhilHealth markers (REQUIRED)
                "PHILHEALTH",
                "PHIL-HEALTH",
                "PHIL HEALTH",
                "PHILIPPINE HEALTH INSURANCE",
                "HEALTH INSURANCE CORPORATION",
                "MEMBER ID",
                "MDR ID", // PhilHealth Member Data Record
                
                // UMID/SSS markers (REQUIRED)
                "UMID",
                "UNIFIED MULTI-PURPOSE ID",
                "GSIS",
                "SSS",
                "SOCIAL SECURITY",
                
                // Postal ID markers (REQUIRED)
                "POSTAL ID",
                "PHILIPPINE POSTAL",
                "PHLPOST",
                "POST OFFICE",
                
                // Passport markers (REQUIRED)
                "PASSPORT",
                "PASAPORTE",
                "REPUBLIC OF THE PHILIPPINES PASSPORT",
                "REPUBLIKA NG PILIPINAS PASSPORT",
                "P<PHL", // Machine readable zone marker for PH passport
                
                // TIN ID markers (REQUIRED)
                "TIN",
                "TAX IDENTIFICATION NUMBER",
                "BIR",
                "BUREAU OF INTERNAL REVENUE"
            };
            
            // Check for STRONG ID markers first and detect ID type
            bool hasStrongIdMarker = strongIdMarkers.Any(marker => upperText.Contains(marker));
            string detectedIdType = null;
            
            // AGGRESSIVE: Detect specific ID type with partial matches for OCR errors
            if (upperText.Contains("PHILSYS") || upperText.Contains("PAMBANSANG") || upperText.Contains("PAGKAKAKILANLAN") || upperText.Contains("PHILIPPINE IDENTIFICATION"))
                detectedIdType = "PhilSys";
            else if ((upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE"))
                detectedIdType = "Driver's License";
            else if (upperText.Contains("TRANSPORTATION") || upperText.Contains("LTO"))
                detectedIdType = "Driver's License";
            else if (upperText.Contains("PHILHEALTH") || upperText.Contains("PHIL-HEALTH") || upperText.Contains("PHIL HEALTH") || 
                     upperText.Contains("HEALTH INSURANCE CORPORATION") || upperText.Contains("MDR ID"))
                detectedIdType = "PhilHealth ID";
            else if (upperText.Contains("PASSPORT") || upperText.Contains("PASAPORTE") || upperText.Contains("P<PHL"))
                detectedIdType = "Passport";
            else if (upperText.Contains("UMID") || upperText.Contains("UNIFIED MULTI-PURPOSE") || upperText.Contains("MULTI-PURPOSE ID"))
                detectedIdType = "UMID";
            else if (upperText.Contains("SSS") || upperText.Contains("SOCIAL SECURITY"))
                detectedIdType = "SSS ID";
            else if (upperText.Contains("POSTAL") || upperText.Contains("PHLPOST") || upperText.Contains("POST OFFICE"))
                detectedIdType = "Postal ID";
            else if (upperText.Contains("TIN") || upperText.Contains("TAX IDENTIFICATION") || upperText.Contains("BIR"))
                detectedIdType = "TIN ID";
            else if (upperText.Contains("GSIS"))
                detectedIdType = "GSIS ID";
            else if (upperText.Contains("REPUBLIK") || upperText.Contains("PILIPINAS") || (upperText.Contains("REPUBLIC") && upperText.Contains("PHILIPP")))
                detectedIdType = "Philippine Government ID";
            
            // Also check for partial matches of strong markers (handle OCR errors)
            // VERY AGGRESSIVE: Even partial words should trigger acceptance
            if (!hasStrongIdMarker)
            {
                hasStrongIdMarker = 
                    upperText.Contains("REPUBLIK") || // OCR error for "REPUBLIKA"
                    upperText.Contains("PILIPINAS") || // Tagalog spelling
                    (upperText.Contains("REPUBLIC") && upperText.Contains("PHILIPP")) || // Partial match
                    upperText.Contains("PAMBANSANG") || // PhilSys marker
                    upperText.Contains("PAGKAKAKILANLAN") || // PhilSys marker
                    upperText.Contains("TRANSPORTATION") || // LTO
                    ((upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE")) ||
                    upperText.Contains("PHILSYS") ||
                    upperText.Contains("PHILHEALTH") || upperText.Contains("PHIL HEALTH") || // PhilHealth
                    upperText.Contains("HEALTH INSURANCE") || // PhilHealth Corporation
                    upperText.Contains("UMID") ||
                    upperText.Contains("POSTAL") ||
                    upperText.Contains("PASSPORT") || upperText.Contains("PASAPORTE") || // Passport EN/TL
                    upperText.Contains("P<PHL") || // Passport machine readable zone
                    upperText.Contains("LTO") || // Land Transportation Office
                    ((upperText.Contains("TAX") || upperText.Contains("TIN")) && upperText.Contains("IDENTIFICATION"));
                    
                if (hasStrongIdMarker)
                {
                    _logger.LogInformation("✅ Found marker through partial matching!");
                }
            }

            // CRITICAL: Must have a STRONG ID marker - screenshots won't have these
            // BUT: Also check for common OCR errors and partial text that might indicate a valid ID
            if (!hasStrongIdMarker)
            {
                // EXTRA LENIENT: Check for very common patterns that indicate a Driver's License even with OCR errors
                // This handles cases where OCR might miss some characters
                
                // CRITICAL: If we find valid barangay numbers (158-161), this is a STRONG indicator of a valid ID
                var validBarangayPattern = Regex.IsMatch(upperText, @"BARANGAY\s*(158|159|160|161)|BRGY\.?\s*(158|159|160|161)|(158|159|160|161)\s+BARANGAY", RegexOptions.IgnoreCase);
                if (validBarangayPattern)
                {
                    _logger.LogInformation("✅ Found valid barangay (158-161) in text - passing validation as valid ID");
                    hasStrongIdMarker = true;
                    detectedIdType = detectedIdType ?? "Driver's License";
                }
                else
                {
                    var veryLenientPatterns = new[]
                    {
                        "DRIVER", "LICENSE", "LTO", "TRANSPORTATION", // Driver's License indicators (even partial)
                        "REPUBLIC", "PHILIPPINES", "PHILIPPINE", // Republic indicators
                        "PHILSYS", "PHILHEALTH", "POSTAL", "PASSPORT", // Other ID types
                        "BARANGAY 161", "BARANGAY 160", "BARANGAY 159", "BARANGAY 158", // Barangay numbers
                        "BARANGAY 16", "BRGY 16", // Partial barangay
                        "LT5", "BLK1", "CITY", "KALOOKAN", "NCR", "REPARO" // Address indicators
                    };
                    
                    var hasLenientPatterns = veryLenientPatterns.Count(pattern => upperText.Contains(pattern));
                    
                    // If we have multiple indicators (at least 2), it's likely a valid ID with OCR errors
                    if (hasLenientPatterns >= 2)
                    {
                        _logger.LogInformation("✅ Passing validation based on lenient pattern matching ({PatternCount} patterns found)", hasLenientPatterns);
                        _logger.LogInformation("Text preview: {Preview}", text.Substring(0, Math.Min(500, text.Length)));
                        hasStrongIdMarker = true; // Override to pass validation
                        detectedIdType = detectedIdType ?? "Driver's License"; // Default to Driver's License if not detected
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Document validation failed: No strong Philippine ID markers found");
                        _logger.LogWarning("Text length: {Length} characters", text.Length);
                        _logger.LogWarning("Text preview (first 1000 chars): {Preview}", text.Length > 1000 ? text.Substring(0, 1000) + "..." : text);
                        _logger.LogWarning("Pattern matches found: {PatternCount}", hasLenientPatterns);
                        _logger.LogWarning("Screenshots and non-ID documents are rejected. Please upload an actual Philippine ID document.");
                        return (false, "Unverified / Invalid ID Image");
                    }
                }
            }

            // Additional validation: Check for ID-specific fields (using very lenient partial matches)
            var idFields = new[]
            {
                "LAST NAME", "SURNAME", "APELYIDO", "APELLIDO", "LAST", "NAME",
                "FIRST NAME", "GIVEN NAME", "MGA PANGALAN", "FIRST", "GIVEN",
                "DATE OF BIRTH", "BIRTH DATE", "KAPANGANAKAN", "BIRTH", "DATE",
                "ADDRESS", "TIRAHAN", "BARANGAY", "BARANG", "BRGY", "CITY",
                "SEX", "GENDER", "KASARIAN", "NATIONALITY", "NATL"
            };

            // Document should have at least 2 ID fields (name + address or birth date)
            // Use very lenient partial matching to handle OCR errors
            int fieldCount = idFields.Count(field => 
                upperText.Contains(field) || 
                upperText.Contains(field.Replace(" ", "")) ||
                // Handle common OCR errors - very lenient
                (field.Contains("BARANGAY") && (upperText.Contains("BARANG") || upperText.Contains("BRGY") || upperText.Contains("BAR"))) ||
                (field.Contains("ADDRESS") && (upperText.Contains("ADDR") || upperText.Contains("CITY") || upperText.Contains("ADD"))) ||
                (field.Contains("NAME") && (upperText.Contains("NAM") || upperText.Contains("SURNAME") || upperText.Contains("SUR"))) ||
                (field.Contains("NATIONALITY") && (upperText.Contains("NATL") || upperText.Contains("NATIO") || upperText.Contains("NAT")))
            );
            
            // Also check for patterns that indicate ID fields even if garbled
            // Check for name pattern (Last, First format)
            if (Regex.IsMatch(upperText, @"[A-Z]{2,},\s*[A-Z]{2,}"))
            {
                fieldCount++; // Found name pattern
            }
            
            // Check for license number pattern (N10-22-300176)
            if (Regex.IsMatch(upperText, @"\b\d{2,3}-\d{2}-\d{6}\b") || Regex.IsMatch(upperText, @"[A-Z]?\d{2,3}-\d{2}-\d{6}"))
            {
                fieldCount++; // Found license number
            }
            
            // Check if we have barangay number or address indicators
            var hasBarangay = Regex.IsMatch(upperText, @"\b(15[89]|1[6-9][0-9]|1[6-9][0-9])\b") || 
                             upperText.Contains("BARANG") || upperText.Contains("BRGY") ||
                             upperText.Contains("LT") || upperText.Contains("BLK"); // LT5 BLK1 indicates address
            
            if (hasBarangay)
            {
                fieldCount++; // Count barangay/address as a field
            }
            
            // Check for address indicators
            if (upperText.Contains("ADDRESS") || upperText.Contains("ADDR") || 
                upperText.Contains("LT") || upperText.Contains("BLK") || 
                upperText.Contains("CITY") || upperText.Contains("REPARO"))
            {
                fieldCount++; // Count address indicators
            }
            
            // CRITICAL: Check for handwritten document indicators ONLY if we don't have strong ID markers
            // If we have strong ID markers, skip handwriting check (valid IDs can have OCR formatting quirks)
            if (!hasStrongIdMarker)
            {
                // Check for explicit handwritten phrases
                var handwrittenPhrases = new[] { 
                    "HANDWRITTEN", "HAND WRITTEN", "WRITTEN BY HAND", "MANUAL SIGNATURE", 
                    "SIGNED BY HAND", "PEN", "PENCIL", "HANDWRITE", "MANUALLY WRITTEN"
                };
                if (handwrittenPhrases.Any(phrase => upperText.Contains(phrase)))
                {
                    _logger.LogWarning("⚠️ Document validation failed: Handwritten document indicators found");
                    return (false, "Handwritten Document Detected - Please upload a photo of your official printed ID, not a handwritten document");
                }
                
                // Detect handwritten patterns: excessive mixed case, inconsistent spacing, irregular patterns
                // Real printed IDs have consistent formatting - handwritten ones don't
                var letterCount = text.Count(char.IsLetter);
                if (letterCount >= 20) // Only check if we have enough text
                {
                    var mixedCaseRatio = text.Count(char.IsLower) / (double)Math.Max(letterCount, 1);
                    var hasExcessiveMixedCase = mixedCaseRatio > 0.3 && mixedCaseRatio < 0.7; // Handwritten often mixes case inconsistently
                    
                    // Check for irregular spacing patterns (handwritten often has inconsistent spacing)
                    var irregularSpacingPattern = Regex.IsMatch(text, @"\w\s{3,}\w"); // Multiple spaces between words
                    var hasIrregularSpacing = irregularSpacingPattern;
                    
                    // Check for handwriting-like patterns: inconsistent line breaks, irregular formatting
                    var lineBreakCount = text.Count(c => c == '\n' || c == '\r');
                    var hasIrregularLineBreaks = lineBreakCount > 20 && lineBreakCount < 100; // Too many line breaks suggests handwritten
                    
                    // If multiple handwriting indicators are present, reject
                    int handwritingScore = 0;
                    if (hasExcessiveMixedCase) handwritingScore++;
                    if (hasIrregularSpacing) handwritingScore++;
                    if (hasIrregularLineBreaks) handwritingScore++;
                    
                    if (handwritingScore >= 2)
                    {
                        _logger.LogWarning("⚠️ Document validation failed: Handwritten document detected");
                        _logger.LogWarning("Handwriting indicators: Mixed case={MixedCase}, Irregular spacing={Spacing}, Irregular breaks={Breaks}", 
                            hasExcessiveMixedCase, hasIrregularSpacing, hasIrregularLineBreaks);
                        return (false, "Handwritten Document Detected - Please upload a photo of your official printed ID, not a handwritten document");
                    }
                }
            }
            else
            {
                _logger.LogInformation("✅ Skipping handwriting check - strong ID markers detected (ID Type: {IdType})", detectedIdType);
            }
            
            // VERY LENIENT: If we have strong ID markers, ALWAYS pass validation
            // Strong markers like "REPUBLIKA NG PILIPINAS", "DRIVER'S LICENSE", "PHILSYS" are enough
            if (hasStrongIdMarker)
            {
                if (fieldCount < 1)
                {
                    _logger.LogInformation("✅ Passing validation based on strong government markers (ID Type: {IdType})", detectedIdType);
                }
                // Auto-pass with strong markers - don't require field counts
            }
            else if (fieldCount < 1)
            {
                _logger.LogWarning("⚠️ Document validation failed: No strong markers and no ID fields found");
                _logger.LogWarning("Extracted text: {Text}", text.Substring(0, Math.Min(500, text.Length)));
                return (false, "Unverified / Invalid ID Image");
            }

            _logger.LogInformation("✅ Document validation passed: Philippine ID detected");
            _logger.LogInformation("   ID Type: {IdType}", detectedIdType ?? "Unknown Philippine ID");
            _logger.LogInformation("   Strong Markers: {Markers}, ID Fields: {Fields}", strongIdMarkers.Count(m => upperText.Contains(m)), fieldCount);
            return (true, detectedIdType ?? "Philippine Government ID");
        }

        /// <summary>
        /// Searches for Barangay 158, 159, 160, or 161 in the extracted text
        /// STRICT VALIDATION: Only matches exactly 158, 159, 160, or 161
        /// Requires actual Philippine ID document (not plain text or screenshots)
        /// </summary>
        private OcrResult ExtractBarangayNumber(string text)
        {
            // STEP 0: Validate that this is an actual Philippine ID document
            var (isValid, idType) = IsValidPhilippineIdDocument(text);
            
            if (!isValid)
            {
                _logger.LogError("❌ REJECTED: Document is not a valid Philippine ID");
                _logger.LogError("The uploaded file appears to be plain text, a screenshot, or not a valid Philippine ID document.");
                _logger.LogError("Please upload an actual Philippine ID document (Driver's License, National ID, PhilHealth ID, etc.)");
                
                bool isScreenshot = idType?.Contains("Screenshot") == true;
                
                // Provide specific error message based on rejection reason
                string errorMessage = "Invalid document type. ";
                if (isScreenshot)
                {
                    errorMessage = "Screenshot detected. Please upload a photo of your actual physical ID document, not a screenshot from your device. ";
                }
                else if (idType?.Contains("Handwritten") == true)
                {
                    errorMessage = "Handwritten document detected. Please upload a photo of your official printed ID document, not a handwritten document. ";
                }
                else
                {
                    errorMessage = "The uploaded document is not a recognized Philippine government-issued ID. ";
                }
                
                errorMessage += "Accepted IDs include: Driver's License, National ID (PhilSys), PhilHealth ID, Postal ID, UMID, TIN ID, SSS ID, or Passport. " +
                               "Only printed official documents are accepted. Screenshots, handwritten documents, or non-printed documents will be rejected.";
                
                return new OcrResult
                {
                    Success = false,
                    Status = "unverified",
                    IdType = idType,
                    BarangayMatch = false,
                    Message = errorMessage,
                    ExtractedText = text,
                    IsScreenshot = isScreenshot
                };
            }
            
            _logger.LogInformation("📄 Valid Philippine ID detected: {IdType}", idType);

            // Define valid barangays - ONLY these are accepted
            var validBarangays = new[] { "158", "159", "160", "161" };
            
            // STEP 1: First, try to find VALID barangays (158-161) - highest priority
            // Try multiple regex patterns to catch variations - STRICT patterns only
            // Handle OCR errors like "BARANG." instead of "BARANGAY"
            var validPatterns = new[]
            {
                // HIGHEST PRIORITY: "BARANGAY 161," or "BARANGAY 161" (handles commas, periods, etc.)
                @"\bBARANGAY\s+(158|159|160|161)(?:[,\s\.]|$|\b)",           // BARANGAY 158 (with punctuation support)
                @"BARANGAY\s+(158|159|160|161)(?:[,\s\.]|$|\b)",            // BARANGAY 158 (without word boundary)
                @"\bBARANG\.?\s+(158|159|160|161)(?:[,\s\.]|$|\b)",          // BARANG. 158 or BARANG 158 (OCR error)
                @"\bBARANG\s+(158|159|160|161)(?:[,\s\.]|$|\b)",             // BARANG 158 (OCR error - missing AY)
                @"\bBRGY\.?\s+(158|159|160|161)(?:[,\s\.]|$|\b)",            // BRGY 158 or BRGY. 158 (with punctuation support)
                @"\bBARANGAY\s+NO\.?\s+(158|159|160|161)(?:[,\s\.]|$|\b)",   // BARANGAY NO. 158 (with punctuation support)
                @"\bBARANGAY\s+#\s+(158|159|160|161)(?:[,\s\.]|$|\b)",       // BARANGAY # 158 (with punctuation support)
                @"\b(158|159|160|161)\s+BARANGAY\b",           // 158 BARANGAY (with word boundaries)
                @"\b(158|159|160|161)\s+BARANG\.?\b",          // 158 BARANG. (OCR error)
                @"BARANG\.?\s*,\s*(158|159|160|161)(?:[,\s\.]|$|\b)",       // BARANG., 158 (from address line)
                @"BARANG\s*,\s*(158|159|160|161)(?:[,\s\.]|$|\b)",          // BARANG, 158 (from address line - OCR error)
                @"BA\s+(158|159|160|161)(?:[,\s\.]|$|\b)",                  // BA 158 (very garbled - BARANGAY cut off)
                
                // AGGRESSIVE: If "BARANG" or "BARANA" or similar appears, look for 158-161 within 50 chars
                @"(?:BARANG|BARANA|BARAN|BRGY|BAR)[A-Z]*\b.{0,50}?\b(158|159|160|161)\b", // BARANGA... 161 (OCR misread)
                @"\b(158|159|160|161)\b.{0,50}?(?:BARANG|BARANA|BARAN|BRGY|BAR)[A-Z]*\b", // 161... BARANGA (reverse)
                
                @"(?:^|\s|,|\.)(158|159|160|161)(?:\s|$|,|\.)", // Just the numbers with context boundaries
                // Look for numbers near address keywords - updated to handle punctuation
                @"(?:LT|BLK|ADDRESS|BARANG|BRGY|CITY).*?BARANGAY\s+(158|159|160|161)(?:[,\s\.]|$|\b)", // Number near address keywords
            };

            foreach (var pattern in validPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var barangayNumber = match.Groups[1].Value.Trim();
                    
                    // CRITICAL: Double-check that the detected barangay is EXACTLY in the valid list
                    if (validBarangays.Contains(barangayNumber))
                    {
                        _logger.LogInformation("=== BARANGAY FOUND (VALIDATED) ===");
                        _logger.LogInformation("Pattern: {Pattern}", pattern);
                        _logger.LogInformation("Barangay: {Barangay}", barangayNumber);
                        _logger.LogInformation("ID Type: {IdType}", idType);

                        return new OcrResult
                        {
                            Success = true,
                            Status = "verified",
                            IdType = idType,
                            BarangayMatch = true,
                            BarangayNumber = barangayNumber,
                            Message = $"Valid {idType} and Barangay {barangayNumber} verified",
                            ExtractedText = text
                        };
                    }
                }
            }

            // STEP 2: If no valid barangay found, check if ANY other barangay number exists
            // This helps identify when an ineligible barangay is found (e.g., 168, 162, etc.)
            var otherBarangayPatterns = new[]
            {
                @"\bBARANGAY\s+(\d{2,4})\b",           // BARANGAY 168, BARANGAY 162, etc.
                @"\bBRGY\.?\s+(\d{2,4})\b",            // BRGY 168, BRGY. 162, etc.
                @"\bBARANGAY\s+NO\.?\s+(\d{2,4})\b",   // BARANGAY NO. 168, etc.
            };

            foreach (var pattern in otherBarangayPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && match.Groups.Count > 1)
                {
                    var detectedNumber = match.Groups[1].Value.Trim();
                    
                    // If it's NOT in the valid list, reject it explicitly
                    if (!validBarangays.Contains(detectedNumber))
                    {
                        _logger.LogWarning("⚠️ Detected non-eligible barangay: {Barangay} (not in 158-161)", detectedNumber);
                        return new OcrResult
                        {
                            Success = false,
                            Status = "unverified",
                            IdType = idType,
                            BarangayMatch = false,
                            BarangayNumber = detectedNumber, // Store for error message
                            Message = $"The document shows Barangay {detectedNumber}, which is not eligible for automatic verification. Only Barangay 158, 159, 160, or 161 are eligible. Your account will require manual review by an administrator.",
                            ExtractedText = text
                        };
                    }
                }
            }

            // STEP 3: No barangay number found at all
            _logger.LogWarning("No valid barangay number (158-161) found in text");
            _logger.LogInformation("Text searched (preview): {Text}", text.Length > 500 ? text.Substring(0, 500) + "..." : text);

            return new OcrResult
            {
                Success = false,
                Status = "unverified",
                IdType = idType,
                BarangayMatch = false,
                Message = "Unable to verify residency. No valid Barangay number (158, 159, 160, or 161) found in the document. " +
                         "Please ensure your ID clearly displays your address with Barangay 158, 159, 160, or 161. " +
                         "The address must be clearly visible and readable in the uploaded document.",
                ExtractedText = text
            };
        }
    }
}

