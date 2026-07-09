using Avalonia.Media;

namespace Clipwell.Ui;

/// <summary>
/// Lucide icon geometry (lucide 1.23.0, ISC license — https://lucide.dev),
/// converted from the source SVGs into path data. Icons are stroke-based: render
/// with a stroked <c>Path</c> (class <c>lucide</c>), never a filled PathIcon.
/// One shared frozen geometry per icon — zero per-row allocation. The webui uses
/// the same icons via lucide-solid at the same version, so the pickers match.
/// </summary>
public static class Icons
{
    /// <summary>lucide `code`.</summary>
    public static readonly StreamGeometry Code =
        StreamGeometry.Parse(
            "M16 18 l6-6-6-6 M8 6 l-6 6 6 6");

    /// <summary>lucide `file-text`.</summary>
    public static readonly StreamGeometry FileText =
        StreamGeometry.Parse(
            "M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z M14 2v5a1 1 0 0 0 1 1h5 M10 9H8 M16 13H8 M16 17H8");

    /// <summary>lucide `folder`.</summary>
    public static readonly StreamGeometry Folder =
        StreamGeometry.Parse(
            "M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z");

    /// <summary>lucide `git-pull-request`.</summary>
    public static readonly StreamGeometry GitPullRequest =
        StreamGeometry.Parse(
            "M15 18 A3 3 0 1 0 21 18 A3 3 0 1 0 15 18 Z M3 6 A3 3 0 1 0 9 6 A3 3 0 1 0 3 6 Z M13 6h3a2 2 0 0 1 2 2v7 M6 9 L6 21");

    /// <summary>lucide `image`.</summary>
    public static readonly StreamGeometry Image =
        StreamGeometry.Parse(
            "M5 3 H19 A2 2 0 0 1 21 5 V19 A2 2 0 0 1 19 21 H5 A2 2 0 0 1 3 19 V5 A2 2 0 0 1 5 3 Z M7 9 A2 2 0 1 0 11 9 A2 2 0 1 0 7 9 Z M21 15 l-3.086-3.086a2 2 0 0 0-2.828 0L6 21");

    /// <summary>lucide `link`.</summary>
    public static readonly StreamGeometry Link =
        StreamGeometry.Parse(
            "M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71 M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71");

    /// <summary>lucide `lock`.</summary>
    public static readonly StreamGeometry Lock =
        StreamGeometry.Parse(
            "M5 11 H19 A2 2 0 0 1 21 13 V20 A2 2 0 0 1 19 22 H5 A2 2 0 0 1 3 20 V13 A2 2 0 0 1 5 11 Z M7 11V7a5 5 0 0 1 10 0v4");

    /// <summary>lucide `mail`.</summary>
    public static readonly StreamGeometry Mail =
        StreamGeometry.Parse(
            "M22 7 l-8.991 5.727a2 2 0 0 1-2.009 0L2 7 M4 4 H20 A2 2 0 0 1 22 6 V18 A2 2 0 0 1 20 20 H4 A2 2 0 0 1 2 18 V6 A2 2 0 0 1 4 4 Z");

    /// <summary>lucide `palette`.</summary>
    public static readonly StreamGeometry Palette =
        StreamGeometry.Parse(
            "M12 22a1 1 0 0 1 0-20 10 9 0 0 1 10 9 5 5 0 0 1-5 5h-2.25a1.75 1.75 0 0 0-1.4 2.8l.3.4a1.75 1.75 0 0 1-1.4 2.8z M13 6.5 A0.5 0.5 0 1 0 14 6.5 A0.5 0.5 0 1 0 13 6.5 Z M17 10.5 A0.5 0.5 0 1 0 18 10.5 A0.5 0.5 0 1 0 17 10.5 Z M6 12.5 A0.5 0.5 0 1 0 7 12.5 A0.5 0.5 0 1 0 6 12.5 Z M8 7.5 A0.5 0.5 0 1 0 9 7.5 A0.5 0.5 0 1 0 8 7.5 Z");

    /// <summary>lucide `panel-right`.</summary>
    public static readonly StreamGeometry PanelRight =
        StreamGeometry.Parse(
            "M5 3 H19 A2 2 0 0 1 21 5 V19 A2 2 0 0 1 19 21 H5 A2 2 0 0 1 3 19 V5 A2 2 0 0 1 5 3 Z M15 3v18");

    /// <summary>lucide `pencil`.</summary>
    public static readonly StreamGeometry Pencil =
        StreamGeometry.Parse(
            "M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z M15 5 l4 4");

    /// <summary>lucide `pin`.</summary>
    public static readonly StreamGeometry Pin =
        StreamGeometry.Parse(
            "M12 17v5 M9 10.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24V16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V7a1 1 0 0 1 1-1 2 2 0 0 0 0-4H8a2 2 0 0 0 0 4 1 1 0 0 1 1 1z");

    /// <summary>lucide `refresh-cw`.</summary>
    public static readonly StreamGeometry RefreshCw =
        StreamGeometry.Parse(
            "M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8 M21 3v5h-5 M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16 M8 16H3v5");

    /// <summary>lucide `rows-3`.</summary>
    public static readonly StreamGeometry Rows3 =
        StreamGeometry.Parse(
            "M5 3 H19 A2 2 0 0 1 21 5 V19 A2 2 0 0 1 19 21 H5 A2 2 0 0 1 3 19 V5 A2 2 0 0 1 5 3 Z M21 9H3 M21 15H3");

    /// <summary>lucide `search`.</summary>
    public static readonly StreamGeometry Search =
        StreamGeometry.Parse(
            "M21 21 l-4.34-4.34 M3 11 A8 8 0 1 0 19 11 A8 8 0 1 0 3 11 Z");

    /// <summary>lucide `square-pen`.</summary>
    public static readonly StreamGeometry SquarePen =
        StreamGeometry.Parse(
            "M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7 M18.375 2.625a1 1 0 0 1 3 3l-9.013 9.014a2 2 0 0 1-.853.505l-2.873.84a.5.5 0 0 1-.62-.62l.84-2.873a2 2 0 0 1 .506-.852z");

    /// <summary>lucide `ticket`.</summary>
    public static readonly StreamGeometry Ticket =
        StreamGeometry.Parse(
            "M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z M13 5v2 M13 17v2 M13 11v2");

    /// <summary>lucide `triangle-alert`.</summary>
    public static readonly StreamGeometry TriangleAlert =
        StreamGeometry.Parse(
            "M21.73 18 l-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3 M12 9v4 M12 17h.01");

    /// <summary>lucide `x`.</summary>
    public static readonly StreamGeometry X =
        StreamGeometry.Parse(
            "M18 6 6 18 M6 6 l12 12");

    /// <summary>The row icon for a detector kind (shared map with the webui).</summary>
    public static StreamGeometry ForKind(string? kind) => kind switch
    {
        "github-pr" => GitPullRequest,
        "jira-issue" => Ticket,
        "url" => Link,
        "email" => Mail,
        "color" => Palette,
        "path" => Folder,
        "code" => Code,
        "image" => Image,
        _ => FileText,
    };
}
