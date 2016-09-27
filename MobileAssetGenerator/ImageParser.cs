using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace MobileAssetGenerator
{
  public class ImageParser
  {
    bool verbose;
    public ImageParser(bool verbose = false)
    {
      this.verbose = verbose;
    }

    Size ComputeTargetSize(Size originalSize, Size requestSize, ResizeOptions resizeOpts)
    {
      if (requestSize.IsZeroOrNegative)
      {
        return originalSize;
      }

      var width = requestSize.Width;
      var height = requestSize.Height;

      double r = originalSize.Width / originalSize.Height;
      if (width <= 0) { width = r * height; }
      if (height <= 0) { height = width / r; }

      switch (resizeOpts)
      {
        case ResizeOptions.Fill:
          break;
        case ResizeOptions.Fit:
          width = Math.Min(width, r * height);
          height = Math.Min(height, width / r);
          break;
        case ResizeOptions.Stretch:
          width = Math.Max(width, r * height);
          height = Math.Max(height, width / height);
          break;
      }

      return new Size(width, height);
    }

    public bool Resize(string inPath, string outPath, Size requestSize = default(Size), double padding = 0, ResizeOptions resizeOpts = ResizeOptions.Fit)
    {
      if (!File.Exists(inPath))
      {
        Console.Error.WriteLine($"ImageParser :: Resize :: The file {inPath} does not exist.");
        return false;
      }

      try
      {
        if (requestSize.IsZeroOrNegative && padding == 0)
        {
          File.Copy(inPath, outPath, true);
          WriteLine($"ImageParser :: Resize :: No resize requested for file {inPath}. We will simply copy it to {outPath}.");
          return true;
        }

        using (var srcImage = Image.FromFile(inPath))
        {
          requestSize = ComputeTargetSize(new Size(srcImage.Width, srcImage.Height), requestSize, resizeOpts);

          int width = (int)(requestSize.Width + 2 * padding);
          int height = (int)(requestSize.Height + 2 * padding);
          using (var newImage = new Bitmap(width, height))
          using (var graphics = Graphics.FromImage(newImage))
          {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(srcImage, new Rectangle((int)padding, (int)padding, (int)requestSize.Width, (int)requestSize.Height));
            newImage.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
          }

          WriteLine($"ImageParser :: Resize :: Successfully resized {inPath}[{srcImage.Width}x{srcImage.Height}] to {outPath}[{width}x{height} (padding: {padding})].");
        }

        return true;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"ImageParser :: Resize :: There was an error resizing the file {inPath} to {outPath}. Details: {ex}");
        return false;
      }
    }

    public bool Resize(string inPath, string outPath, double scale = 1d, double padding = 0d)
    {
      if (!File.Exists(inPath))
      {
        Console.Error.WriteLine($"ImageParser :: Resize :: The file {inPath} does not exist.");
        return false;
      }

      try
      {
        if (Math.Abs(scale - 1d) <= double.Epsilon && padding == 0)
        {
          File.Copy(inPath, outPath, true);
          WriteLine($"ImageParser :: Resize :: No resize requested for file {inPath}. We will simply copy it to {outPath}.");
          return true;
        }

        using (var srcImage = Image.FromFile(inPath))
        {
          int width = (int)(srcImage.Width + 2 * padding);
          int height = (int)(srcImage.Height + 2 * padding);
          using (var newImage = new Bitmap(width, height))
          using (var graphics = Graphics.FromImage(newImage))
          {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(srcImage, new Rectangle((int)padding, (int)padding, srcImage.Width, srcImage.Height));
            newImage.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
          }

          WriteLine($"ImageParser :: Resize :: Successfully resized {inPath}[{srcImage.Width}x{srcImage.Height}] to {outPath}[{width}x{height} (padding: {padding})].");
        }

        return true;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"ImageParser :: Resize :: There was an error resizing the file {inPath} to {outPath}. Details: {ex}");
        return false;
      }
    }

    static readonly IDictionary<AndroidSize, double> AndroidResizeMap = new Dictionary<AndroidSize, double>
    {
      { AndroidSize.MDPI, 1d },
      { AndroidSize.HDPI, 1.5 },
      { AndroidSize.XHDPI, 2d },
      { AndroidSize.XXHDPI, 3d },
      { AndroidSize.XXXHDPI, 4d }
    };

    static readonly IDictionary<AndroidSize, string> AndroidDirMap = new Dictionary<AndroidSize, string>
    {
      { AndroidSize.MDPI, "drawable-mdpi" },
      { AndroidSize.HDPI, "drawable-hdpi" },
      { AndroidSize.XHDPI, "drawable-xdpi" },
      { AndroidSize.XXHDPI, "drawable-xxhdpi" },
      { AndroidSize.XXXHDPI, "drawable-xxxhdpi" }
    };

    public bool ToAndroid(string inFilePath, string outDir, Size targetDp = default(Size), double padding = 0d)
    {
      if (!File.Exists(inFilePath))
      {
        Console.Error.WriteLine($"ImageParser :: ToAndroid :: The file {inFilePath} does not exist.");
        return false;
      }

      try
      {
        var fileName = Path.GetFileName(inFilePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inFilePath);
        var newDir = Path.Combine(outDir, fileNameWithoutExtension);

        foreach (AndroidSize size in Enum.GetValues(typeof(AndroidSize)))
        {
          var dir = Path.Combine(newDir, AndroidDirMap[size]);
          Directory.CreateDirectory(dir);

          var outFilePath = Path.Combine(dir, fileName);
          if (File.Exists(outFilePath)) { File.Delete(outFilePath); }

          if (targetDp.IsZeroOrNegative)
          {
            // Then we have to use the current image as the xxxhdpi (4x) and the rest proportionally
            Resize(inFilePath, outFilePath, AndroidResizeMap[size] / 4d, padding * AndroidResizeMap[size] / 4d);
            return true;
          }
          else
          {
            Resize(inFilePath, outFilePath, targetDp * AndroidResizeMap[size], padding * AndroidResizeMap[size], ResizeOptions.Fit);
          }
        }
        WriteLine($"ImageParser :: ToAndroid :: Successfully created Android directory structure for {inFilePath} into {newDir}.");
        return true;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"ImageParser :: ToAndroid :: There was an error generating the Android file structure for {inFilePath} into {outDir}. Details: {ex}");
        return false;
      }
    }

    public bool ToiOS(string inFilePath, string outDir, Size targetDp = default(Size), double padding = 0d)
    {
      if (!File.Exists(inFilePath))
      {
        Console.Error.WriteLine($"ImageParser :: ToiOS :: The file {inFilePath} does not exist.");
        return false;
      }

      try
      {
        var fileName = Path.GetFileName(inFilePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inFilePath);
        var newDir = Path.Combine(outDir, fileNameWithoutExtension);
        Directory.CreateDirectory(newDir);
        var ext = new Dictionary<int, string> { { 1, "" }, { 2, "@2x" }, { 3, "@3x" } };

        var contentsImages = new JArray();
        for (int x = 1; x <= 3; ++x)
        {
          var outFilePath = Path.Combine(newDir, $"{fileNameWithoutExtension}{ext[x]}.png");
          
          if (File.Exists(outFilePath)) { File.Delete(outFilePath); }

          bool success;
          if (targetDp.IsZeroOrNegative)
          {
            // Then we have to use the current image as the xxxhdpi (4x) and the rest proportionally
            success = Resize(inFilePath, outFilePath, x / 3d, padding * x / 3d);
          }
          else
          {
            success = Resize(inFilePath, outFilePath, targetDp * x, padding * x, ResizeOptions.Fit);
          }

          if (!success) { throw new Exception($"Error resizing {inFilePath} into {outFilePath}"); }

          contentsImages.Add(new JObject(
            new JProperty("filename", Path.GetFileName(outFilePath)),
            new JProperty("idiom", "universal"),
            new JProperty("scale", $"{x}x")));
        }

        // Create Contents.json
        JObject contents = new JObject(
          new JProperty("images", contentsImages),
          new JProperty("info", new JObject(
            new JProperty("author", "xcode"),
            new JProperty("version", 1))));

        var contentsFilePath = Path.Combine(newDir, "Contents.json");
        if (File.Exists(contentsFilePath)) { File.Delete(contentsFilePath); }
        File.WriteAllText(contentsFilePath, contents.ToString(), Encoding.UTF8);

        WriteLine($"ImageParser :: ToiOS :: Successfully created iOS directory structure for {inFilePath} into {newDir}.");
        return true;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"ImageParser :: ToiOS :: There was an error generating the iOS file structure for {inFilePath} into {outDir}. Details: {ex}");
        return false;
      }
    }

    public bool BatchToAndroid(string inDir, string outDir, Size targetDp = default(Size), int padding = 0, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
      try
      {
        var files = Directory.GetFiles(inDir, "*.png", searchOption);
        bool success = true;
        foreach (var file in files)
        {
          success = success && ToAndroid(file, outDir, targetDp, padding);
        }
        WriteLine($"ImageParser :: BatchToAndroid :: Finished creating Android directory structure for files in {inDir} into {outDir}.");
        return success;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"ImageParser :: BatchToAndroid :: There was an error generating in batch the Android file structures for all files in {inDir} into {outDir}. Details: {ex}");
        return false;
      }
    }

    public bool BatchToiOS(string inDir, string outDir, Size targetDp = default(Size), int padding = 0, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
      try
      {
        var files = Directory.GetFiles(inDir, "*.png", searchOption);
        bool success = true;
        foreach (var file in files)
        {
          success = success && ToiOS(file, outDir, targetDp, padding);
        }
        WriteLine($"ImageParser :: BatchToiOS :: Finished creating iOS directory structure for files in {inDir} into {outDir}.");
        return success;
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"ImageParser :: BatchToiOS :: There was an error generating in batch the iOS file structures for all files in {inDir} into {outDir}. Details: {ex}");
        return false;
      }
    }

    public bool GenerateMobileImages(string inDir, string outDir, Size targetDp = default(Size), int padding = 0, SearchOption searchOption = SearchOption.TopDirectoryOnly, bool android = true, bool iOS = true)
    {
      bool ret = true;

      if (android)
      {
        var androidDir = Path.Combine(outDir, "Android");
        ret = ret && BatchToAndroid(inDir, androidDir, targetDp, padding, searchOption);
      }
      if (iOS)
      {
        var iOSDir = Path.Combine(outDir, "iOS");
        ret = ret && BatchToiOS(inDir, iOSDir, targetDp, padding, searchOption);
      }

      WriteLine("ImageParser :: GenerateMobileImages :: Done. {0}", ret ? "All operations completed successfully." : "There were some errors, please look at the log.");
      return ret;
    }

    void WriteLine(string format, params object[] args)
    {
      if (!verbose) { return; }
      Console.WriteLine(format, args);
    }
  }

  public enum ResizeOptions
  {
    Fit,
    Stretch,
    Fill
  }

  public struct Size
  {
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsZeroOrNegative { get { return Width <= 0 && Height <= 0; } }

    public static readonly Size Zero = new Size { Width = 0d, Height = 0d };

    public Size(double width, double height) { Width = width; Height = height; }

    public static Size operator *(Size s, double x) { return new Size(s.Width * x, s.Height * x); }
    public static Size operator *(double x, Size s) { return new Size(s.Width * x, s.Height * x); }
  }

  public enum AndroidSize
  {
    MDPI,
    HDPI,
    XHDPI,
    XXHDPI,
    XXXHDPI
  }
}
