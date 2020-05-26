#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define solutions.
var solutions = new Dictionary<string, string> {
     { "./src/ObjectListView.sln", "Any" },
};

// Define directories.
var buildDir = Directory("./build") + Directory(configuration);

// Define AssemblyInfo source.
var assemblyInfoVersion = ParseAssemblyInfo("./src/.files/AssemblyInfo.Version.cs");

// Define version.
var ticks = DateTime.Now.ToString("ddHHmmss");
var assemblyVersion = assemblyInfoVersion.AssemblyVersion.Replace(".*", "." + ticks.Substring(ticks.Length-8,8));
var version = EnvironmentVariable("APPVEYOR_BUILD_VERSION") ?? Argument("version", assemblyVersion);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
	CleanDirectories("./**/bin");
    CleanDirectories("./**/obj");
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    foreach (var solution in solutions)
    {
        NuGetRestore(solution.Key);
    }
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    foreach (var solution in solutions)
    {
        var settings = new MSBuildSettings();
        settings.SetConfiguration(configuration);
        MSBuild(solution.Key, settings);
    }
});

Task("Build-NuGet-Packages")
    .Does(() =>
    {
        foreach (var folder in new System.IO.FileInfo(solutions.ElementAt(0).Key).Directory.GetDirectories())
        {
            foreach (var file in folder.GetFiles("*.nuspec"))
            {
        		var path = file.Directory;
                var assemblyInfo = ParseAssemblyInfo(path + "/Properties/AssemblyInfo.cs");
                var nuGetPackSettings = new NuGetPackSettings()
                {
                    OutputDirectory = buildDir,
                    IncludeReferencedProjects = false,
                    //Id = assemblyInfo.Title.Replace(" ", "."),
                    //Title = assemblyInfo.Title,
                    Version = version,
                    //Authors = new[] { assemblyInfoCommon.Company },
                    //Summary = assemblyInfo.Description,
                    //Copyright = assemblyInfoCommon.Copyright,
                    Properties = new Dictionary<string, string>()
                    {{ "Configuration", configuration }}
                };
                NuGetPack(file.FullName, nuGetPackSettings);
            }
        }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
        });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Build-NuGet-Packages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
