using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
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

	[Parameter] readonly bool Https;
	
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
				var origin = Https ? repository.HttpsUrl : repository.SshUrl;
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
			var lines = File.ReadAllLines(RootDirectory / "README.md").ToList();

			void Purge()
			{
				for (var i = 0; i < lines.Count; i++)
				{
					if (!lines[i].StartsWith("<!-- BEGIN"))
						continue;

					i++;
					while (i < lines.Count && !lines[i].StartsWith("<!-- END"))
					{
						lines.RemoveAt(i);
					}
				}
			}

			void AddEntries(Category category, IEnumerable<ReadmeEntry> entries)
			{
				var index = lines.FindIndex(x => x.Equals($"<!-- BEGIN {category.ToString().ToUpper()} -->")) + 1;
				foreach (var entry in entries.Reverse())
					lines.Insert(index, $"| {entry.Name} | {entry.Description} |");
				lines.Insert(index, "| --- | --- |");
				lines.Insert(index, "| Name | Description |");
			}
			
			Purge();

			var allEntries = new DirectoryInfo(RepositoriesDirectory)
				.EnumerateFiles("README.md", maxDepth: 3)
				.Select(ReadmeEntry.TryCreate)
				.WhereNotNull()
				.OrderBy(x => x.Name)
				.ToLookup(x => x.Category);
			AddEntries(Category.Common, allEntries[Category.Common]);
			AddEntries(Category.Extensions, allEntries[Category.Extensions]);
			AddEntries(Category.Addons, allEntries[Category.Addons]);

			File.WriteAllLines(RootDirectory / "README.md", lines);
		});

	enum Category
	{
		Common,
		Extensions,
		Addons
	}

	class ReadmeEntry
	{
		[CanBeNull]
		public static ReadmeEntry TryCreate(FileInfo readmeFile)
		{
			var readmeContent = File.ReadAllLines(readmeFile.FullName).ToList();
			var startIndex = readmeContent.FindIndex(x => x.StartsWith("<!-- BEGIN DESCRIPTION -->"));
			var endIndex = readmeContent.FindIndex(x => x.StartsWith("<!-- END DESCRIPTION -->"));

			if (startIndex == -1 && endIndex == -1)
				return null;
			Assert(startIndex > -1 && endIndex > -1, $"Incomplete description section in '{readmeFile}'.");

			var description = readmeContent
				.Skip(startIndex + 1)
				.Take(endIndex - startIndex - 1)
				.Reverse().SkipWhile(string.IsNullOrWhiteSpace)
				.Reverse().SkipWhile(string.IsNullOrWhiteSpace)
				.Join("<br />");

			var repository = GitRepository.FromLocalDirectory(readmeFile.Directory.NotNull().FullName);
			var name = repository.Identifier;
			var category = s_commonRepositories.Contains(name, StringComparer.OrdinalIgnoreCase)
				? Category.Common
				: s_extensionRepositories.Contains(name, StringComparer.OrdinalIgnoreCase)
					? Category.Extensions
					: Category.Addons;

			return new ReadmeEntry
			       {
				       Name = name,
				       Category = category,
				       Description = description,
				       Repository = repository
			       };
		}

		static readonly string[] s_commonRepositories =
		{
			"nuke-build/common"
		};

		static readonly string[] s_extensionRepositories =
		{
			"nuke-build/vscode",
			"nuke-build/resharper"
		};

		public string Name { get; private set; }
		public Category Category { get; private set; }
		public string Description { get; private set; }
		public GitRepository Repository { get; private set; }
	}

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
