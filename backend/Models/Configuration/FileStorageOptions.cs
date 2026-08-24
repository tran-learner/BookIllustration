namespace BookIllustration_Backend.Models.Configuration;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string IllustrationsDirectory { get; set; } =
        "AppData/Illustrations";

    public string BooksDirectory { get; set; } = "AppData/Books";

    public long MaxBookTextPreviewBytes { get; set; } = 1_048_576;
}
