using System.IO.Compression;
using System.Security;
using Prym.Agent.Services;

namespace Prym.Agent.Tests;

/// <summary>Tests for <see cref="UpdateExecutorService.ValidateZipPathTraversal"/>.</summary>
public class ZipPathTraversalTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public ZipPathTraversalTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Safe ZIPs ────────────────────────────────────────────────────────────

    [Fact]
    public void SafeZip_NoException()
    {
        var zipPath = CreateZip([
            ("file.txt", "content"),
            ("subdir/nested.txt", "nested content")
        ]);

        var ex = Record.Exception(() =>
            UpdateExecutorService.ValidateZipPathTraversal(zipPath, _tempDir));

        Assert.Null(ex);
    }

    [Fact]
    public void SafeZip_DirectoryEntries_NoException()
    {
        // Entries whose Name is empty are directory markers; they should be skipped
        var zipPath = CreateZipWithDirectoryEntry();

        var ex = Record.Exception(() =>
            UpdateExecutorService.ValidateZipPathTraversal(zipPath, _tempDir));

        Assert.Null(ex);
    }

    // ── Malicious ZIPs (zip-slip) ─────────────────────────────────────────────

    [Fact]
    public void TraversalWithDotDot_ThrowsSecurityException()
    {
        // Craft an entry whose full name escapes the destination
        var zipPath = CreateRawZipWithEntry("../escape.txt", "evil");

        Assert.Throws<SecurityException>(() =>
            UpdateExecutorService.ValidateZipPathTraversal(zipPath, _tempDir));
    }

    [Fact]
    public void TraversalWithDeepDotDot_ThrowsSecurityException()
    {
        var zipPath = CreateRawZipWithEntry("subdir/../../escape.txt", "evil");

        Assert.Throws<SecurityException>(() =>
            UpdateExecutorService.ValidateZipPathTraversal(zipPath, _tempDir));
    }

    [Fact]
    public void TraversalWithAbsolutePath_ThrowsSecurityException()
    {
        // On Unix this would be "/etc/passwd" style; on Windows "C:\evil".
        // We use a relative path that normalises outside the dest dir.
        var zipPath = CreateRawZipWithEntry("../../../../evil.txt", "evil");

        Assert.Throws<SecurityException>(() =>
            UpdateExecutorService.ValidateZipPathTraversal(zipPath, _tempDir));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a ZIP with well-formed relative entries.</summary>
    private string CreateZip(IEnumerable<(string entryName, string content)> entries)
    {
        var zipPath = Path.Combine(_tempDir, $"{Path.GetRandomFileName()}.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        return zipPath;
    }

    /// <summary>Creates a ZIP with a directory-marker entry (Name=="").</summary>
    private string CreateZipWithDirectoryEntry()
    {
        var zipPath = Path.Combine(_tempDir, $"{Path.GetRandomFileName()}.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        // Directory entries end with '/' and have an empty Name
        archive.CreateEntry("subdir/");
        var e = archive.CreateEntry("subdir/file.txt");
        using var w = new StreamWriter(e.Open());
        w.Write("ok");
        return zipPath;
    }

    /// <summary>
    /// Creates a ZIP whose first entry has an arbitrary (potentially malicious) FullName.
    /// Uses raw ZipArchive so entry names are not sanitised.
    /// </summary>
    private string CreateRawZipWithEntry(string rawEntryName, string content)
    {
        var zipPath = Path.Combine(_tempDir, $"{Path.GetRandomFileName()}.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        // ZipArchiveEntry.FullName is set via CreateEntry; ZipFile sanitises a little,
        // but we use a path that still causes Path.GetFullPath to escape the target dir.
        var entry = archive.CreateEntry(rawEntryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
        return zipPath;
    }
}
