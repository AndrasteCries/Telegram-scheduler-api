using Microsoft.Extensions.Options;
using TdLib;

namespace TelegramScheduler.Services;

public class TelegramMediaService
{
    private readonly MediaStorageOptions _options;

    public TelegramMediaService(IOptions<MediaStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string?> DownloadPhotoAsync(TdClient client, TdApi.File? tdFile, string targetDirectory,
        string fileNameWithoutExt)
    {
        if (tdFile == null)
            return null;

        Directory.CreateDirectory(targetDirectory);

        var file = tdFile;

        if (!file.Local.IsDownloadingCompleted)
        {
            await client.DownloadFileAsync(
                fileId: file.Id,
                priority: 1,
                offset: 0,
                limit: 0,
                synchronous: true
            );
            file = await client.GetFileAsync(file.Id);
        }

        if (!file.Local.IsDownloadingCompleted || string.IsNullOrEmpty(file.Local.Path))
            return null;

        var ext = Path.GetExtension(file.Local.Path);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        var targetPath = Path.Combine(targetDirectory, $"{fileNameWithoutExt}{ext}");

        File.Copy(file.Local.Path, targetPath, overwrite: true);

        return targetPath;
    }

    public string GetChannelAvatarPath(long channelId, string ext = ".jpg")
        => Path.Combine(_options.ChannelAvatarsPath, $"{channelId}{ext}");

    public string GetAccountAvatarPath(long telegramAccountId, string ext = ".jpg")
        => Path.Combine(_options.AccountAvatarsPath, $"{telegramAccountId}{ext}");
}