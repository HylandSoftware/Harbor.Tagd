#addin "nuget:?package=Cake.Docker&version=0.9.3"

const string SOLUTION = "./Harbor.Tagd.sln";

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Clean::Test")
	.Does(() => 
{
	if(DirectoryExists("./_tests"))
	{
		DeleteDirectory("./_tests", new DeleteDirectorySettings {
			Recursive = true
		});
	}
});

Task("Clean::Dist")
	.Does(() => 
{
	if(DirectoryExists("./dist"))
	{
		DeleteDirectory("./dist", new DeleteDirectorySettings {
			Recursive = true
		});
	}
});

Task("Clean")
	.IsDependentOn("Clean::Test")
	.IsDependentOn("Clean::Dist")
	.Does(() =>
{
	DotNetCoreClean(SOLUTION);
});

Task("Restore")
    .Does(() =>
{
    DotNetCoreRestore();
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreBuild(SOLUTION, new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("--no-restore")
    });
});

Task("Test")
    .IsDependentOn("Build")
    .WithCriteria(!Argument("SkipTests", false))
    .Does(() =>
{
    foreach(var proj in GetFiles("./test/**/*.csproj"))
    {
        Information("Testing Project: " + proj);
        DotNetCoreTest(proj.FullPath, new DotNetCoreTestSettings
        {
            Configuration = configuration,
            ArgumentCustomization = args => args.Append("--no-restore")
        });
    }
});

Task("Dist")
    .IsDependentOn("Clean::Dist")
    .IsDependentOn("Test")
    .Does(() => 
{
    DotNetCorePublish("./src/Harbor.Tagd/Harbor.Tagd.csproj", new DotNetCorePublishSettings {
        OutputDirectory = "./dist",
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("--no-restore")
    });
});

Task("Docker")
    .IsDependentOn("Dist")
    .Does(() => 
{
    DockerBuild(new DockerImageBuildSettings {
        Tag = new[]{ "hcr.io/nlowe/tagd:latest" }
    }, ".");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
