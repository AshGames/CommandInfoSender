using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Fonts;

namespace Expediteur.Infrastructure.Pdf;

internal sealed class WindowsFontResolver : IFontResolver
{
    public const string PrimaryFontFamily = "Arial";

    private const string RegularKey = "Arial#Regular";
    private const string BoldKey = "Arial#Bold";

    private static readonly Lazy<WindowsFontResolver> LazyInstance = new(() => new WindowsFontResolver());
    private readonly ConcurrentDictionary<string, byte[]> _fontData = new(StringComparer.OrdinalIgnoreCase);

    private WindowsFontResolver()
    {
        TryLoadFont("arial.ttf", RegularKey);
        TryLoadFont("arialbd.ttf", BoldKey);
    }

    public static void EnsureRegistered()
    {
        if (GlobalFontSettings.FontResolver is WindowsFontResolver)
        {
            return;
        }

        GlobalFontSettings.FontResolver ??= LazyInstance.Value;
    }

    public string DefaultFontName => PrimaryFontFamily;

    public byte[] GetFont(string faceName)
    {
        if (_fontData.TryGetValue(faceName, out var data))
        {
            return data;
        }

        throw new InvalidOperationException($"Impossible de charger la police '{faceName}'.");
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // MigraDoc ne gère pas encore l'italique avec ce resolver ; on retombe sur Regular si besoin.
        var key = isBold ? BoldKey : RegularKey;

        if (!_fontData.ContainsKey(key))
        {
            throw new InvalidOperationException($"La police '{PrimaryFontFamily}' est introuvable sur ce poste.");
        }

        return new FontResolverInfo(key);
    }

    private void TryLoadFont(string fileName, string key)
    {
        var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var path = Path.Combine(fontsDir, fileName);

        if (!File.Exists(path))
        {
            return;
        }

        _fontData[key] = File.ReadAllBytes(path);
    }
}
