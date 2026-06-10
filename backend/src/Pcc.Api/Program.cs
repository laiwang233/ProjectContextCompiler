using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Pcc.Application.Dtos;
using Pcc.Application.Services;
using Pcc.Domain;
using Pcc.Infrastructure;
using Pcc.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPccInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Project Context Compiler API")
        .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
});
app.UseDefaultFiles();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PccDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "OK", timestamp = DateTimeOffset.UtcNow }))
    .WithTags("Health");

var projects = app.MapGroup("/api/projects").WithTags("Projects");
projects.MapPost("/", async (CreateProjectRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
{
    var project = await workflow.CreateProjectAsync(request, ct);
    return Results.Created($"/api/projects/{project.Id}", project);
});
projects.MapGet("/", async ([AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetProjectsAsync(query, ct)));
projects.MapGet("/{projectId:guid}", async (Guid projectId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetProjectAsync(projectId, ct)));
projects.MapPut("/{projectId:guid}", async (Guid projectId, UpdateProjectRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.UpdateProjectAsync(projectId, request, ct)));
projects.MapDelete("/{projectId:guid}", async (Guid projectId, ICompilerWorkflow workflow, CancellationToken ct) =>
{
    await workflow.DeleteProjectAsync(projectId, ct);
    return Results.NoContent();
});

projects.MapPost("/{projectId:guid}/artifacts/paste", async (Guid projectId, PasteArtifactRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Created($"/api/projects/{projectId}/artifacts", await workflow.PasteArtifactAsync(projectId, request, ct)));
projects.MapPost("/{projectId:guid}/artifacts/upload", async (Guid projectId, IFormFile file, ICompilerWorkflow workflow, CancellationToken ct) =>
{
    await using var stream = file.OpenReadStream();
    var artifact = await workflow.UploadArtifactAsync(projectId, stream, file.FileName, file.ContentType, ct);
    return Results.Created($"/api/artifacts/{artifact.Id}", artifact);
}).DisableAntiforgery();
projects.MapGet("/{projectId:guid}/artifacts", async (Guid projectId, [AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetArtifactsAsync(projectId, query, ct)));
projects.MapGet("/{projectId:guid}/content-blocks", async (Guid projectId, [AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetProjectContentBlocksAsync(projectId, query, ct)));
projects.MapPost("/{projectId:guid}/claims/extract", async (Guid projectId, ICompilerWorkflow workflow, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await workflow.ExtractClaimsAsync(projectId, ct));
    }
    catch (ContentBlocksNotVerifiedException ex)
    {
        return Results.BadRequest(new
        {
            error = "ContentBlocksNotVerified",
            message = ex.Message
        });
    }
    catch (NoConfirmedContentBlocksException ex)
    {
        return Results.BadRequest(new
        {
            error = "NoConfirmedContentBlocks",
            message = ex.Message
        });
    }
});
projects.MapGet("/{projectId:guid}/claims", async (Guid projectId, [AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetClaimsAsync(projectId, query, ct)));
projects.MapPut("/{projectId:guid}/content-blocks/review", async (Guid projectId, ReviewContentBlocksRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.ReviewContentBlocksAsync(projectId, request, ct)));
projects.MapPost("/{projectId:guid}/requirements/synthesize", async (Guid projectId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.SynthesizeRequirementsAsync(projectId, ct)));
projects.MapGet("/{projectId:guid}/requirements", async (Guid projectId, [AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetRequirementsAsync(projectId, query, ct)));
projects.MapGet("/{projectId:guid}/tasks", async (Guid projectId, [AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetTasksAsync(projectId, query, ct)));
projects.MapGet("/{projectId:guid}/task-dependencies", async (Guid projectId, Guid? taskId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetTaskDependenciesAsync(projectId, taskId, ct)));
projects.MapPost("/{projectId:guid}/exports/symphony-markdown", async (Guid projectId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.ExportSymphonyMarkdownAsync(projectId, ct)));
projects.MapGet("/{projectId:guid}/exports", async (Guid projectId, [AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetExportsAsync(projectId, query, ct)));
projects.MapGet("/{projectId:guid}/model-runs", async (Guid projectId, [AsParameters] ListQueryRequest query, ICompilerWorkflow workflow, CancellationToken ct) =>
    await QueryResultAsync(() => workflow.GetModelRunsAsync(projectId, query, ct)));

var artifacts = app.MapGroup("/api/artifacts").WithTags("Artifacts");
artifacts.MapGet("/{artifactId:guid}", async (Guid artifactId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetArtifactAsync(artifactId, ct)));
artifacts.MapDelete("/{artifactId:guid}", async (Guid artifactId, ICompilerWorkflow workflow, CancellationToken ct) =>
{
    await workflow.DeleteArtifactAsync(artifactId, ct);
    return Results.NoContent();
});
artifacts.MapPost("/{artifactId:guid}/read", async (Guid artifactId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.ReadArtifactAsync(artifactId, ct)));
artifacts.MapGet("/{artifactId:guid}/content-blocks", async (Guid artifactId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetContentBlocksAsync(artifactId, ct)));

var contentBlocks = app.MapGroup("/api/content-blocks").WithTags("Content Blocks");
contentBlocks.MapPut("/{contentBlockId:guid}/review", async (Guid contentBlockId, ReviewContentBlockRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.ReviewContentBlockAsync(contentBlockId, request, ct)));

var claims = app.MapGroup("/api/claims").WithTags("Claims");
claims.MapGet("/{claimId:guid}", async (Guid claimId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetClaimAsync(claimId, ct)));
claims.MapPut("/{claimId:guid}/ignore", async (Guid claimId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.IgnoreClaimAsync(claimId, ct)));
claims.MapPut("/{claimId:guid}/restore", async (Guid claimId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.RestoreClaimAsync(claimId, ct)));

var requirements = app.MapGroup("/api/requirements").WithTags("Requirements");
requirements.MapGet("/{requirementId:guid}", async (Guid requirementId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetRequirementAsync(requirementId, ct)));
requirements.MapPut("/{requirementId:guid}", async (Guid requirementId, UpdateRequirementRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.UpdateRequirementAsync(requirementId, request, ct)));
requirements.MapPost("/{requirementId:guid}/new-version", async (Guid requirementId, UpdateRequirementRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.UpdateRequirementAsync(requirementId, request, ct)));
requirements.MapPost("/{requirementId:guid}/review", async (Guid requirementId, ReviewRequirementRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.ReviewRequirementAsync(requirementId, request, ct)));
requirements.MapPost("/{requirementId:guid}/tasks/generate", async (Guid requirementId, ICompilerWorkflow workflow, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await workflow.GenerateTasksAsync(requirementId, ct));
    }
    catch (RequirementNotApprovedException ex)
    {
        return Results.BadRequest(new
        {
            error = "RequirementNotApproved",
            message = ex.Message
        });
    }
});

var tasks = app.MapGroup("/api/tasks").WithTags("Tasks");
tasks.MapGet("/{taskId:guid}", async (Guid taskId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetTaskAsync(taskId, ct)));
tasks.MapPut("/{taskId:guid}", async (Guid taskId, UpdateTaskRequest request, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.UpdateTaskAsync(taskId, request, ct)));
tasks.MapPost("/{taskId:guid}/cancel", async (Guid taskId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.CancelTaskAsync(taskId, ct)));

var exports = app.MapGroup("/api/exports").WithTags("Exports");
exports.MapGet("/{exportId:guid}", async (Guid exportId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetExportAsync(exportId, ct)));

var modelRuns = app.MapGroup("/api/model-runs").WithTags("Model Runs");
modelRuns.MapGet("/{modelRunId:guid}", async (Guid modelRunId, ICompilerWorkflow workflow, CancellationToken ct) =>
    Results.Ok(await workflow.GetModelRunAsync(modelRunId, ct)));

app.MapFallbackToFile("/projects/{*path:nonfile}", "index.html");

app.Run();

static async Task<IResult> QueryResultAsync<T>(Func<Task<PagedResult<T>>> action)
{
    try
    {
        return Results.Ok(await action());
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new
        {
            error = "InvalidQuery",
            message = ex.Message
        });
    }
}
