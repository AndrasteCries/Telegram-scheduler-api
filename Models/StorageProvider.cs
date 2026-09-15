namespace TelegramScheduler.Models;

public enum StorageProvider
{
    Local = 0,
    Telegram = 1,
    S3 = 2,
    AzureBlob = 3,
    Minio = 4
}