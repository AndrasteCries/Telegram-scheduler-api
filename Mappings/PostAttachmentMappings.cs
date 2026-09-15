using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Models;
using TelegramScheduler.Services;

public static class PostAttachmentMappings
{
    public static AttachmentResponse ToResponse(this PostAttachment attachment, SignedUrlService signedUrlService)
    {
        var resourcePath = $"/api/attachments/{attachment.Id}/file";
        var url = signedUrlService.GenerateSignedUrl(resourcePath, TimeSpan.FromHours(1));

        return new AttachmentResponse(
            attachment.Id,
            attachment.FileName,
            attachment.MimeType,
            attachment.Size,
            attachment.Order,
            url
        );
    }
}