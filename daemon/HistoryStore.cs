using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clipwell.Daemon.Detectors;
using Clipwell.Protocol;
using Microsoft.Data.Sqlite;

namespace Clipwell.Daemon;

/// <summary>
/// SQLite-backed clipboard history. Ported from the original windows-settings
/// backend (<c>clipboard-store.ts</c>) so the on-disk schema and dedup semantics
/// are unchanged and existing <c>history.db</c> files keep working.
/// </summary>
public sealed class HistoryStore : IDisposable
{
    private readonly string _dbPath;
    private readonly string _settingsPath;
    private readonly SqliteConnection _conn;
    private readonly Lock _gate = new();
    private readonly DetectorRegistry _detectors;
    private readonly MetadataStore _meta;

    /// <summary>Raised with the detector id when a detector throws while classifying.</summary>
    public event Action<string, Exception>? DetectorFailed;

    /// <param name="meta">The pin/sensitive/alias/edit overlay.</param>
    /// <param name="dataDir">Directory to store into; defaults to <see cref="DataPaths.Resolve"/>.</param>
    /// <param name="detectors">
    /// Extra detectors on top of the built-ins; defaults to whatever
    /// <see cref="Clipwell.Protocol.Plugins.PluginLoader"/> finds in the plugins dir.
    /// </param>
    public HistoryStore(
        MetadataStore meta,
        string? dataDir = null,
        IEnumerable<Clipwell.Protocol.Plugins.IClipDetector>? detectors = null)
    {
        _meta = meta;
        _detectors = new DetectorRegistry(
            detectors ?? Clipwell.Protocol.Plugins.PluginLoader.Load<Clipwell.Protocol.Plugins.IClipDetector>());
        _detectors.Failed += (detectorId, error) => DetectorFailed?.Invoke(detectorId, error);
        var storeDir = string.IsNullOrEmpty(dataDir) ? DataPaths.Resolve() : dataDir;
        Directory.CreateDirectory(storeDir);
        _dbPath = Path.Combine(storeDir, "history.db");
        _settingsPath = Path.Combine(storeDir, "clipboard-settings.json");
        CacheDir = Path.Combine(storeDir, "cache");
        Directory.CreateDirectory(CacheDir);

        _conn = new SqliteConnection($"Data Source={_dbPath}");
        _conn.Open();
        Exec("PRAGMA journal_mode = WAL;");
        Exec("PRAGMA synchronous = NORMAL;");
        Exec("""
            CREATE TABLE IF NOT EXISTS items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp TEXT NOT NULL,
                text_content TEXT,
                text_length INTEGER NOT NULL DEFAULT 0,
                html_content TEXT,
                has_image INTEGER NOT NULL DEFAULT 0,
                image_path TEXT,
                source_app TEXT,
                formats_json TEXT,
                text_sha1 TEXT,
                UNIQUE(timestamp, text_sha1)
            );
            CREATE INDEX IF NOT EXISTS idx_items_ts ON items(timestamp DESC);
            """);
    }

    public string DbPath => _dbPath;

    /// <summary>Directory for cached images captured from the clipboard.</summary>
    public string CacheDir { get; }

    // ── Write ───────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts a capture, or merges it into an existing row sharing the same
    /// (timestamp, text hash). Returns true if a new row was created.
    /// </summary>
    public bool Upsert(StoreRow row)
    {
        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO items
                    (timestamp, text_content, text_length, html_content, has_image,
                     image_path, source_app, formats_json, text_sha1)
                VALUES ($ts, $text, $len, $html, $img, $path, $src, $formats, $sha1)
                ON CONFLICT(timestamp, text_sha1) DO UPDATE SET
                    text_length = excluded.text_length,
                    html_content = COALESCE(excluded.html_content, items.html_content),
                    has_image = CASE WHEN excluded.has_image = 1 THEN 1 ELSE items.has_image END,
                    image_path = COALESCE(excluded.image_path, items.image_path),
                    source_app = COALESCE(NULLIF(excluded.source_app, ''), items.source_app),
                    formats_json = excluded.formats_json;
                """;
            cmd.Parameters.AddWithValue("$ts", row.Timestamp);
            cmd.Parameters.AddWithValue("$text", (object?)row.TextContent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$len", row.TextLength);
            cmd.Parameters.AddWithValue("$html", (object?)row.HtmlContent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$img", row.HasImage ? 1 : 0);
            cmd.Parameters.AddWithValue("$path", (object?)row.ImagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$src", (object?)row.SourceApp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$formats", JsonSerializer.Serialize(row.Formats));
            cmd.Parameters.AddWithValue("$sha1", TextHashFor(row.TextContent));
            return cmd.ExecuteNonQuery() > 0;
        }
    }

    // ── Read ────────────────────────────────────────────────────────────

    public IReadOnlyList<ClipItem> QueryPage(int limit, string? beforeTimestamp)
    {
        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            if (beforeTimestamp is not null)
            {
                cmd.CommandText =
                    "SELECT * FROM items WHERE timestamp < $before ORDER BY timestamp DESC LIMIT $limit";
                cmd.Parameters.AddWithValue("$before", beforeTimestamp);
            }
            else
            {
                cmd.CommandText = "SELECT * FROM items ORDER BY timestamp DESC LIMIT $limit";
            }
            cmd.Parameters.AddWithValue("$limit", limit);

            var items = new List<ClipItem>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var item = Hydrate(RowToItem(reader));
                if (item.HasImage && reader["image_path"] is string imagePath &&
                    ImageSizeFor(imagePath) is (int width, int height))
                {
                    item = item with { ImageWidth = width, ImageHeight = height };
                }
                items.Add(item);
            }
            return items;
        }
    }

    /// <summary>
    /// Full-history text search, newest first. Matches the item's text or its
    /// alias, case-insensitively — the same rule the pickers and
    /// <see cref="GetCounts"/> use. An empty query matches nothing rather than
    /// everything, so a caller that forgets to validate can't dump the history.
    /// </summary>
    public IReadOnlyList<ClipItem> Search(string? query, int limit)
    {
        var needle = query?.Trim();
        if (string.IsNullOrEmpty(needle) || limit <= 0) return [];

        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM items ORDER BY timestamp DESC";
            using var reader = cmd.ExecuteReader();

            var matches = new List<ClipItem>();
            while (reader.Read() && matches.Count < limit)
            {
                var item = ApplyEdit(RowToItem(reader));
                if (!Matches(item, needle)) continue;
                matches.Add(Classify(item));
            }
            return matches;
        }
    }

    /// <summary>Looks up a single item by its exact timestamp, or null.</summary>
    public ClipItem? FindByTimestamp(string timestamp)
    {
        if (string.IsNullOrEmpty(timestamp)) return null;

        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM items WHERE timestamp = $ts LIMIT 1";
            cmd.Parameters.AddWithValue("$ts", timestamp);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Hydrate(RowToItem(reader)) : null;
        }
    }

    /// <summary>
    /// Aggregate counts over the whole history — total, pinned, sensitive, and
    /// per-kind — optionally scoped to a search query using the same match rule
    /// as the pickers (text or alias contains, case-insensitive).
    /// </summary>
    public ClipCounts GetCounts(string? query)
    {
        var needle = query?.Trim();
        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM items ORDER BY timestamp DESC";
            using var reader = cmd.ExecuteReader();

            int total = 0, pinned = 0, sensitive = 0;
            var kinds = new Dictionary<string, int>();
            while (reader.Read())
            {
                // Classification runs a regex chain per row, so filter first and
                // only classify the rows that survive the query.
                var item = ApplyEdit(RowToItem(reader));
                if (!string.IsNullOrEmpty(needle) && !Matches(item, needle)) continue;
                total++;
                if (_meta.IsPinned(item.Timestamp)) pinned++;
                if (_meta.IsSensitive(item.Timestamp)) sensitive++;
                var kind = _detectors.Classify(item);
                kinds[kind] = kinds.GetValueOrDefault(kind) + 1;
            }
            return new ClipCounts { Total = total, Pinned = pinned, Sensitive = sensitive, Kinds = kinds };
        }
    }

    /// <summary>The pickers' match rule: text or alias contains, case-insensitively.</summary>
    private bool Matches(ClipItem item, string needle) =>
        item.TextContent?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true ||
        _meta.Alias(item.Timestamp)?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Replaces the text with the user's non-destructive edit override, if any.
    /// The stale HTML capture is dropped so it can't shadow the edit.
    /// </summary>
    private ClipItem ApplyEdit(ClipItem item) =>
        _meta.Edit(item.Timestamp) is string edit
            ? item with
            {
                TextContent = edit,
                TextLength = edit.Length,
                HtmlContent = null,
                IsEdited = true,
            }
            : item;

    /// <summary>Attaches the detector kind and the user metadata flags.</summary>
    private ClipItem Classify(ClipItem item) => item with
    {
        Kind = _detectors.Classify(item),
        IsUserPinned = _meta.IsPinned(item.Timestamp),
        IsSensitive = _meta.IsSensitive(item.Timestamp),
        Alias = _meta.Alias(item.Timestamp),
    };

    /// <summary>
    /// Applies the user's edit override then classifies — so edited text
    /// re-detects its kind rather than keeping the original capture's.
    /// </summary>
    private ClipItem Hydrate(ClipItem item) => Classify(ApplyEdit(item));

    // ── Retention / clear ───────────────────────────────────────────────

    /// <summary>
    /// Deletes everything captured before the retention cutoff, except items the
    /// user pinned — pinning is the documented way to keep something past
    /// retention. A null retention means keep forever and deletes nothing.
    /// </summary>
    /// <remarks>
    /// Compares parsed instants, not raw strings: rows written by this daemon use
    /// <c>DateTimeOffset "o"</c> (<c>+00:00</c>) while rows inherited from the
    /// legacy store use a <c>Z</c> suffix, and those two sort differently as text.
    /// A row whose timestamp SQLite cannot parse yields NULL, which fails the
    /// comparison and is therefore kept — the safe direction for a destructive
    /// sweep.
    /// </remarks>
    public int SweepOlderThan(int? retentionDays)
    {
        if (retentionDays is null || retentionDays.Value < 0) return 0;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays.Value).ToString("o");
        lock (_gate)
        {
            var expired = new List<string>();
            using (var select = _conn.CreateCommand())
            {
                select.CommandText =
                    "SELECT timestamp FROM items WHERE strftime('%s', timestamp) < strftime('%s', $cutoff)";
                select.Parameters.AddWithValue("$cutoff", cutoff);
                using var reader = select.ExecuteReader();
                while (reader.Read()) expired.Add(reader.GetString(0));
            }

            var pinned = _meta.PinnedTimestamps();
            var doomed = expired.Where(timestamp => !pinned.Contains(timestamp)).ToList();
            if (doomed.Count == 0) return 0;

            using (var delete = _conn.CreateCommand())
            {
                delete.CommandText = "DELETE FROM items WHERE timestamp = $ts";
                var parameter = delete.Parameters.Add("$ts", Microsoft.Data.Sqlite.SqliteType.Text);
                using var tx = _conn.BeginTransaction();
                delete.Transaction = tx;
                foreach (var timestamp in doomed)
                {
                    parameter.Value = timestamp;
                    delete.ExecuteNonQuery();
                }
                tx.Commit();
            }

            foreach (var timestamp in doomed) _meta.Forget(timestamp);
            Exec("PRAGMA optimize;");
            return doomed.Count;
        }
    }

    /// <summary>
    /// Deletes all history. Metadata goes with it — otherwise a later capture
    /// landing on a purged item's timestamp would inherit its alias, pin, and
    /// sensitive flag.
    /// </summary>
    public int ClearAll()
    {
        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM items";
            var deleted = cmd.ExecuteNonQuery();
            _meta.Clear();
            return deleted;
        }
    }

    public string? GetImagePath(string timestamp)
    {
        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT image_path FROM items WHERE timestamp = $ts AND image_path IS NOT NULL LIMIT 1";
            cmd.Parameters.AddWithValue("$ts", timestamp);
            return cmd.ExecuteScalar() as string;
        }
    }

    public bool DeleteByTimestamp(string timestamp)
    {
        lock (_gate)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM items WHERE timestamp = $ts";
            cmd.Parameters.AddWithValue("$ts", timestamp);
            var deleted = cmd.ExecuteNonQuery() > 0;
            if (deleted) _meta.Forget(timestamp);
            return deleted;
        }
    }

    // ── Settings ────────────────────────────────────────────────────────

    public ClipboardSettings LoadSettings()
    {
        try
        {
            var raw = File.ReadAllText(_settingsPath);
            var parsed = JsonSerializer.Deserialize<ClipboardSettings>(raw, JsonOpts);
            if (parsed is not null && ClipboardSettings.ValidRetentions.Contains(parsed.RetentionDays))
                return parsed;
        }
        catch
        {
            // Missing or corrupt file → defaults.
        }
        return new ClipboardSettings();
    }

    public void SaveSettings(ClipboardSettings settings)
    {
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOpts));
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private void Exec(string sql)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static ClipItem RowToItem(SqliteDataReader row)
    {
        var formatsJson = row["formats_json"] as string;
        IReadOnlyList<string> formats = [];
        if (!string.IsNullOrEmpty(formatsJson))
        {
            try
            {
                formats = JsonSerializer.Deserialize<List<string>>(formatsJson) ?? [];
            }
            catch (JsonException)
            {
                // corrupt row — empty format list
            }
        }

        return new ClipItem
        {
            // The row's own primary key, so an item's id is stable across pages
            // and across calls (a page-relative index is neither).
            Id = $"db:{row["id"]}",
            Timestamp = (string)row["timestamp"],
            Formats = formats,
            TextContent = row["text_content"] as string,
            TextLength = row["text_length"] is long length ? (int)length : 0,
            HtmlContent = row["html_content"] as string,
            HasImage = row["has_image"] is long hasImage && hasImage == 1,
            IsPinned = false,
            IsUserPinned = false,
            IsSensitive = false,
            SourceApp = row["source_app"] as string ?? "",
        };
    }

    // PNG pixel size per cached image, memoized by path (the cache dir is
    // append-only, so a parsed size never goes stale). Guarded by _gate.
    private readonly Dictionary<string, (int Width, int Height)?> _imageSizes = [];

    private (int Width, int Height)? ImageSizeFor(string path)
    {
        if (_imageSizes.TryGetValue(path, out var cached)) return cached;
        var size = ReadPngSize(path);
        _imageSizes[path] = size;
        return size;
    }

    // Reads width/height from the PNG IHDR chunk (bytes 16..23, big-endian)
    // without decoding the image.
    private static (int Width, int Height)? ReadPngSize(string path)
    {
        try
        {
            Span<byte> header = stackalloc byte[24];
            using var fs = File.OpenRead(path);
            if (fs.ReadAtLeast(header, 24, throwOnEndOfStream: false) < 24) return null;
            if (header[0] != 0x89 || header[1] != (byte)'P' || header[2] != (byte)'N' || header[3] != (byte)'G')
                return null;
            var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
            var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
            return width > 0 && height > 0 ? (width, height) : null;
        }
        catch
        {
            return null; // missing/corrupt cache file — dims are optional
        }
    }

    // Empty string and null hash distinctly so same-timestamp items with
    // different formats do not collide on the UNIQUE key.
    private static string TextHashFor(string? textContent) =>
        Sha1(textContent ?? "__NULL__");

    private static string Sha1(string s) =>
        Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(s)));

    public void Dispose() => _conn.Dispose();
}
