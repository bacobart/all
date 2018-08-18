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

class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.Prepare);

	[Parameter] readonly bool UseSsh;
	
	string Solution => RootDirectory / "nuke.sln";
	AbsolutePath RepositoriesDirectory => RootDirectory / "repositories";
	string RepositoriesConfigurationFile => RootDirectory / "repositories.yml";

	Target Checkout => _ => _
		.Executes(() =>
		{
			var repositories = YamlDeserializeFromFile<string[]>(RepositoriesConfigurationFile)
				.Select(x => x.Split(separator: '#'))
				.Select(x => GitRepository.FromUrl(url: x.First(), branch: x.ElementAtOrDefault(index: 1)));

			foreach (var repository in repositories)
			{
				var repositoryDirectory = RepositoriesDirectory / repository.Identifier;
				var origin = UseSsh ? repository.SshUrl : repository.HttpsUrl;
				var branch = repository.Branch ?? "master";

				if (!Directory.Exists(repositoryDirectory))
					Git($"clone {origin} {repositoryDirectory} --branch {branch} --progress");
				else
					Git($"remote set-url origin {origin}", repositoryDirectory);
			}
		});

	Target Readme => _ => _
		.Executes(() =>
		{
			IReadOnlyCollection<string> GetGlobalSection(string readmeFile)
			{
				var readmeContent = File.ReadAllLines(readmeFile).ToList();
				var startIndex = readmeContent.FindIndex(x => x.StartsWith("<!-- BEGIN GLOBAL SECTION -->"));
				var endIndex = readmeContent.FindIndex(x => x.StartsWith("<!-- END GLOBAL SECTION -->"));

				if (startIndex == -1 && endIndex == -1)
					return new List<string>();
				Assert(startIndex > -1 && endIndex > -1, "Incomplete defined global section in '{readmeFile}'.");

				readmeContent.RemoveRange(endIndex, readmeContent.Count - endIndex);
				readmeContent.RemoveRange(index: 0, count: startIndex);
				
				return readmeContent;
			}

			var readmeFiles = new DirectoryInfo(RepositoriesDirectory)
				.EnumerateFiles("README.md", maxDepth: 3)
				.Select(x => x.FullName)
				.Select(GetGlobalSection);
		});

	Target Prepare => _ => _
		.DependsOn(Checkout)
        .Executes(() =>
        {
            using (var fileStream = File.Create(Solution))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
VisualStudioVersion = 15.0.26124.0
MinimumVisualStudioVersion = 15.0.26124.0

Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""_build"", ""build\_build.csproj"", ""{594DB7F8-B1EA-4C9C-BF63-D12A361C513B}""
EndProject
");

	            string GetSolutionFolderName(string s)
	            {
		            var directoryInfo = new FileInfo(s).Directory.NotNull();
		            return $"{directoryInfo.Parent.NotNull().Name}/{directoryInfo.Name}";
	            }

	            var solutions = GlobFiles(RepositoriesDirectory, "*/*/*.sln")
	                .Where(x => x != Solution)
                    .Select(x => new
	                {
		                File = x,
		                Directory = GetRootRelativePath(new FileInfo(x).Directory.NotNull().FullName),
		                Name = GetSolutionFolderName(x),
		                Guid = Guid.NewGuid().ToString("D").ToUpper(), 
		                Content = ReadAllLines(x)
	                })
	                .OrderBy(x => x.Name).ToList();
                foreach (var solution in solutions)
                {
	                streamWriter.WriteLine(@"Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = " +
	                                       $@"""{solution.Name}"", ""{solution.Name}"", ""{{{solution.Guid}}}""");
                    streamWriter.WriteLine("EndProject");

	                string FixLocation(string line)
	                {
		                if (line.StartsWith("Project"))
		                {
			                var index = line.Select((x, i) => (x, i)).Where(x => x.Item1 == '"').ElementAt(4).Item2;
			                return line.Insert(index + 1, $"{solution.Directory}\\");
		                }
		                
		                if (line.StartsWith("\t\t"))
		                {
			                var index = line.IndexOf('=');
			                return line.Insert(index + 2, $"{solution.Directory}\\");
		                }

		                return line;
	                }
	                
                    solution.Content
                        .SkipWhile(x => !x.StartsWith("Project"))
                        .TakeWhile(x => !x.StartsWith("Global"))
                        // ReSharper disable once AccessToDisposedClosure
                        .ForEach(x => streamWriter.WriteLine(FixLocation(x)));
                }

	            streamWriter.Write(@"
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|x64 = Debug|x64
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|x64 = Release|x64
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
");
	            
	            var guidDictionary = new Dictionary<string, (string SolutionFile, string Declaration)>();
	            foreach (var solution in solutions)
	            {
		            string GetAndTrackGuid(string declaration)
		            {
			            var guid = declaration.Substring(declaration.Length - 38, 36);
			            if (guidDictionary.TryGetValue(guid, out var value))
			            {
				            WriteAllText(
					            solution.File,
					            ReadAllText(solution.File)
						            .Replace(guid, Guid.NewGuid().ToString("D").ToUpper()));
				            
				            Fail(new[]
				            {
					            $"Guid {guid} is duplicated in:",
					            $"  {solution.File}",
					            $"    {declaration}",
					            $"  {value.SolutionFile}",
					            $"    {value.Declaration}",
					            "Guid has been replaced. Restart target."
				            }.JoinNewLine());
			            }
			            guidDictionary.Add(guid, (solution.File, declaration));
			            return guid;
		            }
		            
		            solution.Content
			            .Where(x => x.StartsWith("Project"))
			            .Select(GetAndTrackGuid)
			            // ReSharper disable once AccessToDisposedClosure
			            .ForEach(x => streamWriter.WriteLine($"\t\t{{{x}}} = {{{solution.Guid}}}"));
	            }
	            
	            streamWriter.Write(@"
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{594DB7F8-B1EA-4C9C-BF63-D12A361C513B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{594DB7F8-B1EA-4C9C-BF63-D12A361C513B}.Release|Any CPU.ActiveCfg = Release|Any CPU
");
	            
	            foreach (var solution in solutions)
	            {
		            solution.Content
			            .SkipWhile(x => !x.Contains("GlobalSection(ProjectConfigurationPlatforms)"))
			            .Skip(1)
			            .TakeWhile(x => !x.Contains("EndGlobalSection"))
			            // ReSharper disable once AccessToDisposedClosure
			            .ForEach(x => streamWriter.WriteLine(x));
	            }
	            
	            streamWriter.Write(@"
	EndGlobalSection
EndGlobal");
            }
        });
}
