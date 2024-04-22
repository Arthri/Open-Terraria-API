using AsmAnalyzer;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Templates;

var isVerbose = Environment.GetEnvironmentVariable("VERBOSE_ANALYZER") == "1";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(isVerbose ? LogEventLevel.Verbose : LogEventLevel.Information)
    .WriteTo.Console(
        new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] "
            + "{#if Scope is not null}"
            + "[{#each s in Scope}{s}{#delimit} > {#end}] "
            + "{#end}"
            + "{@m:lj}\n{@x}"
        )
    )
    .WriteTo.File(
        new ExpressionTemplate(
            "{@t:yyyy-MM-dd HH:mm:ss.fff zzz} [{@l:u3}] "
            + "{#if Scope is not null}"
            + "[{#each s in Scope}{s}{#delimit} > {#end}] "
            + "{#end}"
            + "{@m:lj}\n{@x}"
        ),
        $"analysis_{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}.txt",
        rollOnFileSizeLimit: true
    )
    .CreateLogger()
    ;
var logFactory = new SerilogLoggerFactory();

var logger = logFactory.CreateLogger("Main");

var filesToAnalyze = args;
foreach (var file in filesToAnalyze)
{
    logger.LogInformation("Attempting to read {File}...", file);
    AssemblyDefinition? assembly = null;
    try
    {
        assembly = AssemblyDefinition.FromFile(Path.GetFullPath(file));
    }
    catch (Exception e)
    {
        logger.LogError(e, "Error while reading assembly");
        continue;
    }

    logger.LogInformation("Read assembly {Assembly}", assembly.FullName);
    using (logger.BeginScope("{AssemblyName}", assembly.Name?.ToString() ?? "<Unnamed>"))
    {
        try
        {
            foreach (var module in assembly.Modules)
            {
                using var _ = logger.BeginScope("{ModuleName}", module.Name?.ToString() ?? "<Unnamed>");
                try
                {
                    var flaggedReferences = new HashSet<MetadataToken>();
                    using (logger.BeginScope("TypeRef Table"))
                    {
                        foreach (var typeReference in module.GetImportedTypeReferences())
                        {
                            if (typeReference.Scope == null)
                            {
                                logger.LogError("Type reference 0x{MetadataToken}(RID: {RID}) references {ReferencedType} with a null resolution scope.", typeReference.MetadataToken, typeReference.MetadataToken.Rid, typeReference.FullName);
                                flaggedReferences.Add(typeReference.MetadataToken);
                            }
                            else if (typeReference.Scope.MetadataToken == MetadataToken.Zero)
                            {
                                logger.LogError("Type reference 0x{MetadataToken}(RID: {RID}) references {ReferencedType} with a zero resolution scope.", typeReference.MetadataToken, typeReference.MetadataToken.Rid, typeReference.FullName);
                                flaggedReferences.Add(typeReference.MetadataToken);
                            }
                            else if (typeReference.Scope.MetadataToken == module.MetadataToken)
                            {
                                logger.LogError("Type reference 0x{MetadataToken}(RID: {RID}) references {ReferencedType}, a type in the same assembly.", typeReference.MetadataToken, typeReference.MetadataToken.Rid, typeReference.FullName);
                                flaggedReferences.Add(typeReference.MetadataToken);
                            }
                        }
                    }

                    new ModuleAnalyzer(logger, module, flaggedReferences).Analyze();
                }
                catch (Exception e) when (Run(() => logger.LogError(e, "Error during analysis of module")))
                {
                }
            }
        }
        catch (Exception e) when (Run(() => logger.LogError(e, "Error during analysis of assembly")))
        {
        }
    }
}
