using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileAssetGenerator
{
  class Program
  {
    public static void Main(string[] args)
    {
      bool showHelp = false;
      string inDir = null, outDir = null;
      double width = 0, height = 0;
      int padding = 0;
      bool verbose = false;
      SearchOption searchOption = SearchOption.TopDirectoryOnly;

      var opts = new OptionSet() {
        { "i|input=", "the full path of the input directory",
          i => inDir = i },
        { "o|output=", "the output directory",
          o => outDir = o },
        { "w|width=", "the target dp width of result excluding padding (omit to compute automatically based on height)",
          (double w) => width = w },
        { "h|height=", "the target dp height of result excluding padding (omit to compute automatically based on width)",
          (double h) => height = h },
        { "p|padding=", "the target dp padding",
          (int p) => padding = p },
        { "r|recursive",  "recurse subdirectories",
          r => searchOption = r != null ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly },
        { "?|help",  "show this message and exit",
          v => showHelp = v != null },
        { "v|verbose",  "show debug messages",
          v => verbose = v != null }
      };

      string file = typeof(Program).Assembly.Lo‌​cation;
      string exe = Path.GetFileNameWithoutExtension(file);
      List<string> extra;
      try
      {
        extra = opts.Parse(args);
      }
      catch (OptionException e)
      {
        Console.Write($"{exe}: ");
        Console.WriteLine(e.Message);
        Console.WriteLine($"Try '{exe} --help' for more information.");
        return;
      }

      if (showHelp)
      {
        ShowHelp(exe, opts);
        return;
      }

      if (inDir == null)
      {
        Console.Error.WriteLine("No input directory specified.");
        ShowHelp(exe, opts);
        return;
      }

      if (outDir == null)
      {
        Console.Error.WriteLine("No output directory specified.");
        ShowHelp(exe, opts);
        return;
      }

      var parser = new ImageParser(verbose);
      parser.GenerateMobileImages(inDir, outDir, new Size(width, height), padding, searchOption);
    }

    static void ShowHelp(string exe, OptionSet p)
    {
      Console.WriteLine($"Usage: {exe} -i <input dir> -o <output dir> [other options]");
      Console.WriteLine();
      Console.WriteLine("Options:");
      p.WriteOptionDescriptions(Console.Out);
    }
  }
}
