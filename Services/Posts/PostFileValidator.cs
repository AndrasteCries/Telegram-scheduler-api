using TelegramScheduler.Services.Exeptions;

namespace TelegramScheduler.Services.Posts;

public class PostFileValidator
{
    private const int MaxFiles = 10;
    private const long MaxFileSize = 20 * 1024 * 1024;

    private static readonly string[] AllowedMimeTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "video/mp4"
    ];

    public void ValidateCount(int count)
    {
        if (count > MaxFiles)
        {
            throw new ValidationException(
                "Maximum 10 files allowed.");
        }
    }

    public void ValidateFile(IFormFile file)
    {
        if (file.Length > MaxFileSize)
        {
            throw new ValidationException(
                $"File {file.FileName} is too large.");
        }

        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            throw new ValidationException(
                $"Unsupported file type: {file.ContentType}");
        }
    }

    public void ValidateFiles(
        IReadOnlyCollection<IFormFile> files)
    {
        ValidateCount(files.Count);

        foreach (var file in files)
        {
            ValidateFile(file);
        }
    }
}