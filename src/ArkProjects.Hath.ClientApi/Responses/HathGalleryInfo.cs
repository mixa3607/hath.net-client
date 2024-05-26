using System.Text.RegularExpressions;
using System.Text;

namespace ArkProjects.Hath.ClientApi.Responses;

public class HathGalleryInfo
{
    public int GalleryId { get; set; }
    public string MinXRes { get; set; } = "";
    public string Title { get; set; } = "";
    public string About { get; set; } = "";
    public IReadOnlyList<HathGalleryPage> Pages { get; set; } = Array.Empty<HathGalleryPage>();

    public static HathGalleryInfo Parse(IReadOnlyList<string> lines)
    {
        var gallery = new HathGalleryInfo();
        var pages = Array.Empty<HathGalleryPage>();
        var aboutSb = new StringBuilder();
        var parseState = GalleryParseState.Info;

        foreach (var line in lines)
        {
            if (line == "FILELIST" && parseState == GalleryParseState.Info)
            {
                parseState = GalleryParseState.Files;
                continue;
            }
            else if (line == "INFORMATION" && parseState == GalleryParseState.Files)
            {
                parseState = GalleryParseState.About;
                continue;
            }
            else if (line == "" && parseState is GalleryParseState.Files or GalleryParseState.Info)
            {
                continue;
            }

            if (parseState == GalleryParseState.Info)
            {
                var parts = line.Split(' ', 2);
                if (parts[0] == "GID")
                {
                    gallery.GalleryId = int.Parse(parts[1]);
                }
                else if (parts[0] == "FILECOUNT")
                {
                    pages = new HathGalleryPage[int.Parse(parts[1])];
                }
                else if (parts[0] == "MINXRES")
                {
                    if (!Regex.IsMatch(parts[1], "^org|\\d+$"))
                        throw new Exception("Min X res not matched template");
                    gallery.MinXRes = parts[1];
                }
                else if (parts[0] == "TITLE")
                {
                    gallery.Title = parts[1];
                }
            }
            else if (parseState == GalleryParseState.Files)
            {
                var parts = line.Split(' ');
                var page = int.Parse(parts[0]);
                pages[page - 1] = new HathGalleryPage()
                {
                    FileIndex = int.Parse(parts[1]),
                    XRes = parts[2],
                    Hash = parts[3] == "unknown" ? null : parts[3],
                    Type = parts[4],
                    FileName = parts[5]
                };
            }
            else
            {
                aboutSb.AppendLine(line);
            }
        }

        gallery.Pages = pages;
        gallery.About = aboutSb.ToString();
        return gallery;
    }

    private enum GalleryParseState
    {
        Info = 0,
        Files = 1,
        About = 2,
    }
}