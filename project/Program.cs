using LibGit2Sharp;
using Newtonsoft.Json;
using PlasticGitter;

Console.WriteLine("Plastic to Github activity history converter initialized");
var basePath = AppDomain.CurrentDomain.BaseDirectory;
var json = File.ReadAllText(Path.Combine(basePath, "repos.json"));
var repos = JsonConvert.DeserializeObject<List<Repo>>(json);
foreach (var repo in repos)
{
    Console.WriteLine($"Name: {repo.Name}, WorkspacePath: {repo.WorkspacePath}, GithubPath: {repo.GithubPath}");
    repo.Init();
    repo.WritePlasticHistoryToFile();
    repo.CommitToLocal();
    repo.PushToGithub();
}


Console.WriteLine("Complete. Press any key to exit.");
Console.ReadKey();