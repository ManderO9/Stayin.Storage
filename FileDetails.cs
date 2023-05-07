namespace Stayin.Storage;

/// <summary>
/// Details about a file that we are storing in our application
/// </summary>
public class FileDetails
{
    /// <summary>
    /// The unique identifier of this file
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The name of the file
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// File extension of the file
    /// </summary>
    public required string Extension { get; set; }

    /// <summary>
    /// The size in bytes of the file contents
    /// </summary>
    public required int Size { get; set; }
}
