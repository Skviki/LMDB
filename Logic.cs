using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

public static class Logic
{
    public static async Task<string> Build(
        string pkg, string app, string ver, string arch, string auth,
        string desc, string folder, string exeName, string icon,
        string inst, string category)
    {
        await Task.Delay(1000);

        try
        {
            pkg = pkg.Trim().ToLowerInvariant();
            app = app.Trim();
            ver = ver.Trim();
            arch = arch.Trim().ToLowerInvariant();
            auth = auth.Trim();
            desc = desc.Trim();
            folder = folder.Trim();
            exeName = exeName.Trim();
            icon = icon.Trim();
            inst = inst.Trim();
            category = category.Trim();

            var error = Validate(pkg, app, ver, arch, auth, desc, folder,
                exeName, icon, inst, category);

            if (!string.IsNullOrEmpty(error))
                return error;

            if (!await Exists("dpkg-deb"))
                return "dpkg-deb not found. Please install dpkg-dev.";

            var docs = Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

            if (string.IsNullOrWhiteSpace(docs))
                return "Documents directory not found.";

            Directory.CreateDirectory(docs);

            var temp = Path.Combine(
                docs, $"{pkg}_deb_build_{Guid.NewGuid():N}");

            var output = Path.Combine(
                docs, $"{pkg}_{ver}_{arch}.deb");

            Directory.CreateDirectory(temp);

            try
            {
                if (File.Exists(output))
                    File.Delete(output);

                var debian = Path.Combine(temp, "DEBIAN");
                Directory.CreateDirectory(debian);

                await File.WriteAllTextAsync(
                    Path.Combine(debian, "control"),
                    CreateControl(pkg, ver, arch, auth, desc),
                    new UTF8Encoding(false));

                var normalizedInst = NormalizeInstallPath(inst);

                if (string.IsNullOrEmpty(normalizedInst))
                    return "Invalid install path.";

                var programDir = Path.Combine(temp, normalizedInst);
                Directory.CreateDirectory(programDir);

                CopyDirectory(folder, programDir);

                var destinationExe = Path.Combine(programDir, exeName);

                if (!File.Exists(destinationExe))
                    return $"Executable was not found after copying:\n{destinationExe}";

                try
                {
                    File.SetUnixFileMode(
                        destinationExe,
                        UnixFileMode.UserRead |
                        UnixFileMode.UserWrite |
                        UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead |
                        UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead |
                        UnixFileMode.OtherExecute);
                }
                catch (PlatformNotSupportedException)
                {
                }

                var iconName = "";

                if (!string.IsNullOrWhiteSpace(icon))
                {
                    var ext = Path.GetExtension(icon).ToLowerInvariant();

                    if (ext != ".png" && ext != ".svg" && ext != ".xpm")
                        return "Unsupported icon format. Use PNG, SVG or XPM.";

                    iconName = pkg;

                    var iconDir = Path.Combine(
                        temp, "usr", "share", "icons", "hicolor",
                        ext == ".svg" ? "scalable" : "256x256",
                        "apps");

                    Directory.CreateDirectory(iconDir);
                    File.Copy(icon, Path.Combine(iconDir, iconName + ext), true);
                }

                var appDir = Path.Combine(
                    temp, "usr", "share", "applications");

                Directory.CreateDirectory(appDir);

                await File.WriteAllTextAsync(
                    Path.Combine(appDir, $"{pkg}.desktop"),
                    CreateDesktop(app, desc, inst, exeName, iconName, category),
                    new UTF8Encoding(false));

                try
                {
                    await Run("dpkg-deb", "--build", temp, output);
                    await Run("dpkg-deb", "--info", output);
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (File.Exists(output))
                            File.Delete(output);
                    }
                    catch
                    {
                    }

                    return $"DEB build/verification failed:\n{ex.Message}";
                }

                return File.Exists(output)
                    ? ""
                    : "dpkg-deb completed successfully, but the .deb file was not created.";
            }
            finally
            {
                try
                {
                    if (Directory.Exists(temp))
                        Directory.Delete(temp, true);
                }
                catch
                {
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return "Insufficient permissions.";
        }
        catch (DirectoryNotFoundException)
        {
            return "Required directory was not found.";
        }
        catch (FileNotFoundException ex)
        {
            return $"File not found:\n{ex.FileName}";
        }
        catch (IOException ex)
        {
            return $"File system error:\n{ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Unexpected error:\n{ex.Message}";
        }
    }

    static string NormalizeInstallPath(string path)
    {
        path = path.Trim();

        if (string.IsNullOrWhiteSpace(path) ||
            !path.StartsWith('/') ||
            path.Contains(".."))
            return "";

        return path.Trim('/');
    }

    static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
    }

    static string Validate(
        string pkg, string app, string ver, string arch, string auth,
        string desc, string folder, string exeName, string icon,
        string inst, string category)
    {
        if (string.IsNullOrWhiteSpace(pkg) ||
            !Regex.IsMatch(pkg, "^[a-z0-9][a-z0-9.+\\-]{0,99}$"))
            return "Invalid package name.";

        if (string.IsNullOrWhiteSpace(app))
            return "App name missing.";

        if (string.IsNullOrWhiteSpace(ver) ||
            !Regex.IsMatch(ver, "^[a-zA-Z0-9.+\\-:~]{1,100}$"))
            return "Invalid version.";

        if (!new[]
            {
                "amd64", "arm64", "armhf", "i386", "all",
                "riscv64", "ppc64el", "s390x"
            }.Contains(arch))
            return "Unsupported architecture.";

        if (string.IsNullOrWhiteSpace(auth))
            return "Author missing.";

        if (string.IsNullOrWhiteSpace(folder) ||
            !Directory.Exists(folder))
            return "Folder not found.";

        if (string.IsNullOrWhiteSpace(exeName) ||
            !File.Exists(Path.Combine(folder, exeName)))
            return "Executable not found in folder.";

        if (!string.IsNullOrWhiteSpace(icon) &&
            !File.Exists(icon))
            return "Icon not found.";

        if (string.IsNullOrWhiteSpace(inst) ||
            !inst.StartsWith('/') ||
            inst.Contains(".."))
            return "Invalid install path.";

        var categories = new[]
        {
            "AudioVideo", "Audio", "Video", "Development",
            "Education", "Game", "Graphics", "Network",
            "Office", "Science", "Settings", "System",
            "Utility", "Other"
        };

        if (!categories.Contains(
            category,
            StringComparer.OrdinalIgnoreCase))
            return "Invalid category.";

        return "";
    }

    static string CreateControl(
        string pkg, string ver, string arch, string auth, string desc)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Package: {pkg}");
        sb.AppendLine($"Version: {ver}");
        sb.AppendLine($"Architecture: {arch}");
        sb.AppendLine(
            $"Maintainer: {auth.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ")}");

        var lines = desc
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        if (string.IsNullOrWhiteSpace(lines[0]))
        {
            sb.AppendLine("Description: No description provided");
        }
        else
        {
            sb.AppendLine(
                $"Description: {lines[0].Replace("\t", " ")}");

            foreach (var line in lines.Skip(1))
                sb.AppendLine(
                    string.IsNullOrWhiteSpace(line)
                        ? " ."
                        : $" {line.TrimEnd().Replace("\t", " ")}");
        }

        return sb.ToString();
    }

    static string CreateDesktop(
        string app, string desc, string inst,
        string exe, string icon, string category)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[Desktop Entry]");
        sb.AppendLine("Version=1.0");
        sb.AppendLine($"Name={EscapeDesktopValue(app)}");

        if (!string.IsNullOrWhiteSpace(desc))
            sb.AppendLine($"Comment={EscapeDesktopValue(desc)}");

        var execPath = $"{inst.TrimEnd('/')}/{exe}";

        sb.AppendLine($"Exec={EscapeDesktopExec(execPath)}");

        if (!string.IsNullOrWhiteSpace(icon))
            sb.AppendLine($"Icon={icon}");

        sb.AppendLine("Terminal=false");
        sb.AppendLine("Type=Application");
        sb.AppendLine($"Categories={category};");
        sb.AppendLine("StartupNotify=true");

        return sb.ToString();
    }

    static string EscapeDesktopValue(string value) =>
        value.Replace("\\", "\\\\")
             .Replace("\r", " ")
             .Replace("\n", " ")
             .Replace("\t", " ");

    static string EscapeDesktopExec(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    static async Task<bool> Exists(string cmd)
    {
        var psi = new ProcessStartInfo("which", cmd)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);

        if (process == null)
            return false;

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    static async Task Run(string file, params string[] args)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);

        if (process == null)
            throw new Exception($"Failed to start {file}");

        var error = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception((await error).Trim());
    }
}
