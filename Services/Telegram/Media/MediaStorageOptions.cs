namespace TelegramScheduler.Services;
public class MediaStorageOptions
{
    public string RootPath { get; set; } = "storage/media";

    public string ChannelAvatarsPath => Path.Combine(RootPath, "channel-avatars");
    public string AccountAvatarsPath => Path.Combine(RootPath, "account-avatars");
    public string PostAttachmentsPath => Path.Combine(RootPath, "post-attachments");
    public string DownloadedPostMediaPath => Path.Combine(RootPath, "downloaded-post-media");
}