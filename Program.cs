using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Stayin.Storage;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Ensure database exists
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();


app.MapPost("/file/upload",
    async (IConfiguration configuration,
            ApplicationDbContext dbContext,
            HttpResponse response,
            [FromBody] FileInformation fileInfo) =>
    {
        // The folder to store files in (ends with a trailing slash)
        var storageFolder = configuration["StorageFolder"];

        // Create file type provider
        var fileExtensionProvider = new FileExtensionContentTypeProvider();

        // Try get the file extension that maps to the passed in mime type
        var extensions = fileExtensionProvider.Mappings.Where(x => x.Value.Equals(fileInfo.FileType, StringComparison.OrdinalIgnoreCase));

        // If the passed in mime type is unknown
        if(extensions.Count() == 0)
        {
            // Set error status code
            response.StatusCode = StatusCodes.Status400BadRequest;
            
            // Return an error
            await response.WriteAsync($"Unknown Mime Type : {fileInfo.FileType}");

            // End response here
            return;
        }
        
        // Get file extension
        var fileExtension = extensions.First().Key;

        // Check if it's text and set it directly (cuz multiple mime types have .txt extension)
        if(extensions.First().Value == MediaTypeNames.Text.Plain)
            fileExtension = ".txt";
        
        // Create a random name for the file
        var fileName = Guid.NewGuid().ToString("N");

        // Create the path to store the file in
        var path = storageFolder + fileName + fileExtension;

        // Store the file on disc
        await File.WriteAllBytesAsync(path, fileInfo.Content);

        // Create file info to store in the database
        var newFile = new FileDetails()
        {
            Extension = fileExtension,
            Id = Guid.NewGuid().ToString("N"),
            Name = fileName,
            Size = fileInfo.Content.Count()
        };

        // Add the file info to the database
        dbContext.Files.Add(newFile);
        await dbContext.SaveChangesAsync();

        // Return file id to the caller
        await response.WriteAsync(newFile.Id);
    });



app.MapGet("/file/{fileId}", async (ApplicationDbContext db, IConfiguration configuration, string fileId) =>
{
    // Get the file using it's id
    var file = db.Files.FirstOrDefault(x => x.Id == fileId);

    // If the file is null, it doesn't exist
    if(file is null)
        // Return not found
        return Results.NotFound();

    // The folder to store files in (ends with a trailing slash)
    var storageFolder = configuration["StorageFolder"];

    // The path to the file to return
    var path = storageFolder + file.Name + file.Extension;

    // Get the content of the file
    var fileContent = await File.ReadAllBytesAsync(path);

    // Return the file
    return Results.File(fileContent, fileDownloadName: file.Name + file.Extension);
});

app.Run();
