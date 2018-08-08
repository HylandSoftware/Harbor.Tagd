#addin "nuget:?package=Cake.Docker&version=0.9.4"
#addin "nuget:?package=Cake.MiniCover&version=0.29.0-next20180721071547&prerelease"

const string SOLUTION = "./Harbor.Tagd.sln";
SetMiniCoverToolsProject("./minicover/minicover.csproj");

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var tag = Argument("tag", "latest");

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
    MiniCover(tool =>
        {
            foreach(var proj in GetFiles("./test/**/*.csproj"))
            {
                Information("Testing Project: " + proj);
                DotNetCoreTest(proj.FullPath, new DotNetCoreTestSettings
                {
                    NoBuild = true,
                    Configuration = configuration,
                    ArgumentCustomization = args => args.Append("--no-restore")
                });
            }
        },
        new MiniCoverSettings()
            .WithAssembliesMatching("./test/**/*.dll")
            .WithSourcesMatching("./src/**/*.cs")
            .WithNonFatalThreshold()
            .GenerateReport(ReportType.CONSOLE)
    );
});

Task("Coveralls")
    .WithCriteria(TravisCI.IsRunningOnTravisCI)
    .Does(() => 
{
    MiniCoverReport(new MiniCoverSettings()
        .WithCoverallsSettings(c => c.UseTravisDefaults())
        .GenerateReport(ReportType.COVERALLS)
    );
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

Task("Docker::Build")
    .IsDependentOn("Dist")
    .Does(() => 
{
    DockerBuild(new DockerImageBuildSettings {
        Tag = new[]{ $"hylandsoftware/tagd:{tag}" }
    }, ".");
});

Task("Docker::Push")
    .IsDependentOn("Docker::Build")
    .WithCriteria(TravisCI.IsRunningOnTravisCI)
    .Does(() =>
{
    DockerPush($"hylandsoftware/tagd:{tag}");
    
    if (tag != "latest") {
        DockerTag($"hylandsoftware/tagd:{tag}", "hylandsoftware/tagd:latest");
        DockerPush($"hylandsoftware/tagd:latest");
    }
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
