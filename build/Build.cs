using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.Git;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.ControlFlow;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.SerializationTasks;
using static Nuke.Common.IO.TextTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static TeamCityManager;

partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Prepare);

    [Parameter] readonly bool Https;

    string Solution => RootDirectory / "nuke.sln";
    AbsolutePath RepositoriesDirectory => RootDirectory / "repositories";

    IEnumerable<GitRepository> Repositories =>
        YamlDeserializeFromFile<string[]>(RootDirectory / "repositories.yml")
            .Select(x => x.Split(separator: '#'))
            .Select(x => GitRepository.FromUrl(url: x.First(), branch: x.ElementAtOrDefault(index: 1) ?? "master"));

    Target Checkout => _ => _
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
        });

    Target Readme => _ => _
        .Executes(() =>
        {
            ReadmeManager.WriteReadme(RootDirectory / "README.md", RepositoriesDirectory);
        });

    Target Prepare => _ => _
        .DependsOn(Checkout)
        .Executes(() =>
        {
            PrepareSolution();
        });

    string TeamCityConfiguration => RootDirectory / ".teamcity" / "settings.kts";
    
    Target TeamCity => _ => _
        .Executes(() =>
        {
            WriteTeamCityConfiguration(TeamCityConfiguration, Repositories.ToList());
        });
}