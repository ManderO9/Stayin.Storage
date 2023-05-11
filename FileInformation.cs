public record FileInformation
{
    public required string FileType { get; set; }
    public required byte[] Content { get; set; }
}
