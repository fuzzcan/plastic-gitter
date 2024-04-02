using System.Diagnostics;
using System.Security.Cryptography;
using LibGit2Sharp;
namespace PlasticGitter;

public class Repo(string name, string workspacePath, string githubPath, string author, string email)
{
    public readonly string Name = name;
    public readonly string WorkspacePath = workspacePath;
    public readonly string GithubPath = githubPath;
    public readonly string Author = author;
    public readonly string Email = email;

    private string OutputDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Name);
    private string PlasticHistoryOutput => Path.Combine(OutputDir, "history.txt");
    private string CommitsFile => Path.Combine(OutputDir, "commits.txt");

    public void Init()
    {
        if (Directory.Exists(OutputDir))
        {
            RemoveReadOnlyAttribute(OutputDir);
            Directory.Delete(OutputDir, true);
        }

        Directory.CreateDirectory(OutputDir);
    }

    static void RemoveReadOnlyAttribute(string directoryPath)
    {
        // Remove ReadOnly attribute from the directory itself
        var directoryInfo = new DirectoryInfo(directoryPath);
        if (directoryInfo.Exists && (directoryInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
        }

        // Recursively remove ReadOnly attribute from all subdirectories
        foreach (var subDirectory in directoryInfo.GetDirectories())
        {
            RemoveReadOnlyAttribute(subDirectory.FullName);
        }

        // Remove ReadOnly attribute from all files in the directory
        foreach (var file in directoryInfo.GetFiles())
        {
            if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }

    public void WritePlasticHistoryToFile()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-NoProfile -ExecutionPolicy Unrestricted -Command \"cd '{WorkspacePath}'; " +
                    $"cm find changesets \\\"where owner='chris@radicalrobotics.ca'\\\" " +
                    $"| Out-File -FilePath '{PlasticHistoryOutput}'\"",

                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();
        Console.WriteLine($"Saved {Name} PlasticSCM history to: {PlasticHistoryOutput}");
    }


    public void PushToGithub()
    {
        var commands = $"cd '{EscapeForBash(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Name))}' && " +
                       $"eval $(ssh-agent -s) && " +
                       $"ssh-add ~/.ssh/github && " +
                       $"rm -rf git && " +
                       // $"mkdir git && " +
                       // $"cd git && " +
                       $"git remote add origin {GithubPath} && " +
                       $"git push -u origin main --force";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = $"--exec bash -c \"{commands}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();
        process.Close();

        Console.WriteLine("Output: " + output);
        if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine("Error: " + error);
    }

    private static string EscapeForBash(string path)
    {
        path = path.Replace("C:\\", "/mnt/c/");
        path = path.Replace("\\", "/");
        // Escape the path for use in bash command
        return path;
    }

    public void CommitToLocal()
    {
        int commitCounter = 0;
        string commitsFilePath = Path.Combine(OutputDir, "commits.txt");
        File.Create(commitsFilePath).Close();
        Repository.Init(OutputDir);
        var commitLines = File.ReadAllLines(PlasticHistoryOutput);
        Console.WriteLine($"File read successfully. Total lines: {commitLines.Length}");

        using var repo = new Repository(OutputDir);
        foreach (var line in commitLines)
        {
            var sections = line.Split(' ');
            if (!int.TryParse(sections[0], out var _)) continue;
            var dateTime = Utilities.ExtractDateTime(line);
            Console.WriteLine("Commit dt: " + dateTime);
            if (dateTime == null)
            {
                continue;
            }

            File.AppendAllLines(commitsFilePath, new[] { $"Commit #{commitCounter++}: {line}" });
            Commands.Stage(repo, commitsFilePath);


            var author = new Signature(Author, Email, dateTime.Value);
            repo.Commit(line, author, author);
        }
    }
}