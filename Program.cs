using Microsoft.EntityFrameworkCore;
using Stayin.Storage;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Ensure database exists
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();



// TODO: delete later
app.MapGet("/", async (context) =>
{

    context.Response.ContentType = "text/html";

    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(
   """
    <form action="/file" method="post">
        select a file<input type="file">
        <input type="submit">
    </form>
    """
    ));
});

app.MapPost("/file", async (context) =>
{
    // The folder to store files in (ends with a trailing slash)
    var storageFolder = context.RequestServices.GetRequiredService<IConfiguration>()["StorageFolder"];

    // TODO: Get the data to store
    var data = Encoding.UTF8.GetBytes("some content in the file in form of bytes");
    var fileExtension = "txt";
    var fileName = Guid.NewGuid().ToString("N");
    var path = storageFolder + fileName + "." + fileExtension;

    // Store the file on disc
    await File.WriteAllBytesAsync(path, data);

    // Create file info to store in the database
    var newFile = new FileDetails()
    {
        Extension = fileExtension,
        Id = Guid.NewGuid().ToString("N"),
        Name = fileName,
        Size = data.Count()
    };

    // Get the database context
    var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();

    // Add the file info to the database
    db.Files.Add(newFile);
    await db.SaveChangesAsync();

    // Return file id to the caller
    await context.Response.WriteAsync(newFile.Id);
});



app.MapGet("/file/{fileId}", async (ApplicationDbContext db,IConfiguration configuration, string fileId) =>
{
    // Get the file using it's id
    var file = db.Files.FirstOrDefault(x=>x.Id == fileId);

    // If the file is null, it doesn't exist
    if(file is null)
        // Return not found
        return Results.NotFound();

    // The folder to store files in (ends with a trailing slash)
    var storageFolder = configuration["StorageFolder"];

    // The path to the file to return
    var path = storageFolder + file.Name + "." + file.Extension;

    // Get the content of the file
    var fileContent = await File.ReadAllBytesAsync(path);

    // Return the file
    return Results.File(fileContent, fileDownloadName: file.Name + "." + file.Extension);
});

app.Run();
