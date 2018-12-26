using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.ControlFlow;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.SerializationTasks;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.Logger;
using static Nuke.Common.Tools.Git.GitTasks;
using static TeamCityManager;

partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.CreateSolution);

    [Parameter] readonly bool Https;

    string Solution => RootDirectory / "nuke.sln";
    string Organization => "nuke-build";
    
    AbsolutePath RepositoriesDirectory => RootDirectory / "repositories";
    AbsolutePath RepositoriesFile => RootDirectory / "repositories.yml";
    IEnumerable<GitRepository> Repositories =>
        YamlDeserializeFromFile<string[]>(RepositoriesFile)
            .Select(x => x.Split(separator: '#'))
            .Select(x => GitRepository.FromUrl(url: x.First(), branch: x.ElementAtOrDefault(index: 1) ?? "master"));

    Target CreateSolution => _ => _
        .Executes(() =>
        {
            foreach (var repository in Repositories)
            {
                var repositoryDirectory = RepositoriesDirectory / repository.Identifier;
                var origin = Https ? repository.HttpsUrl : repository.SshUrl;
                var branch = repository.Branch;

                if (!Directory.Exists(repositoryDirectory))
                    Git($"clone {origin} {repositoryDirectory} --branch {branch} --progress");
                else
                    Git($"remote set-url origin {origin}", repositoryDirectory);
            }
            
            PrepareSolution();
        });

    [Parameter(Name = "name")] readonly string PascalName;
    [Parameter] string LispName;
    [Parameter] readonly string[] Description;
    [Parameter] readonly string DefaultBranch = "master";
    [PathExecutable] readonly Tool Hub;
    
    Target AddProject => _ => _
        .Requires(() => PascalName)
        .Requires(() => Description)
        .Executes(() =>
        {
            LispName = LispName ?? PascalName.SplitCamelHumpsWithSeparator("-");
            var repositoryDirectory = RepositoriesDirectory / Organization / LispName;
            using (SwitchWorkingDirectory(repositoryDirectory))
            {
                CopyTemplate(repositoryDirectory);
                EnsureCleanDirectory(repositoryDirectory / ".git");

                Git("init");
                Git($"checkout -b {DefaultBranch}");
                Git($"commit -m {"Initialize repository".DoubleQuote()} --allow-empty");
                Hub($"create {Organization}/{LispName} -d {Description.JoinSpace().DoubleQuoteIfNeeded()} -h https://nuke.build");
                Git("add .");
                Git($"commit -m {"Add template files".DoubleQuote()}");
            }

            var updatedRepositories = Repositories
                .Concat(GitRepository.FromUrl($"https://github.com/{Organization}/{LispName}", DefaultBranch))
                .Select(x => $"{x.HttpsUrl}").OrderBy(x => x).ToList();
            YamlSerializeToFile(updatedRepositories, RepositoriesFile);

            ExecuteWithRetry(PrepareSolution, retryAttempts: 5);
        });

    void CopyTemplate(AbsolutePath repositoryDirectory)
    {
        var templateDirectory = RepositoriesDirectory / Organization / "template";
        CopyDirectoryRecursively(templateDirectory, repositoryDirectory);

        var replacements = new Dictionary<string, string>
                           {
                               { "Template", PascalName },
                               { "template", LispName }
                           };
        new[]
        {
            (RelativePath) ".nuke",
            (RelativePath) "nuke-template.sln",
            (RelativePath) "src" / "Nuke.Template.Tests" / "Nuke.Template.Tests.csproj"
        }.ForEach(x => FillTemplateFile(repositoryDirectory / x, replacements: replacements));

        GlobDirectories(repositoryDirectory, "**/Nuke.*").ToList()
            .ForEach(x => Directory.Move(x, x.Replace("Template", PascalName)));
        GlobFiles(repositoryDirectory, "**/Nuke.*").ToList()
            .ForEach(x => File.Move(x, x.Replace("Template", PascalName)));
        GlobFiles(repositoryDirectory, "nuke-*").ToList()
            .ForEach(x => File.Move(x, x.Replace("template", LispName)));
    }

    Target Readme => _ => _
        .Executes(() =>
        {
            ReadmeManager.WriteReadme(RootDirectory / "README.md", RepositoriesDirectory);
        });

    string TeamCityConfiguration => RootDirectory / ".teamcity" / "settings.kts";
    
    Target CreateTeamCity => _ => _
        .Executes(() =>
        {
            WriteTeamCityConfiguration(TeamCityConfiguration, Repositories.ToList());
        });

    IDisposable SwitchWorkingDirectory(string workingDirectory, bool allowCreate = true)
    {
        if (!Directory.Exists(workingDirectory))
            EnsureCleanDirectory(workingDirectory);

        var previousWorkingDirectory = EnvironmentInfo.WorkingDirectory;
        return DelegateDisposable.CreateBracket(
            () => Directory.SetCurrentDirectory(workingDirectory),
            () => Directory.SetCurrentDirectory(previousWorkingDirectory));
    }

    void FillTemplateFile(
        string templateFile,
        IReadOnlyCollection<string> definitions = null,
        IReadOnlyDictionary<string, string> replacements = null)
    {
        var templateContent = ReadAllText(templateFile);
        WriteAllText(templateFile, TemplateUtility.FillTemplate(templateContent, definitions, replacements));
    }
}