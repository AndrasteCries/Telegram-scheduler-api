using Microsoft.EntityFrameworkCore;
using TdLib;
using TelegramScheduler.Data;
using TelegramScheduler.Models;

namespace TelegramScheduler.Services;

public class NotifierService
{
    private readonly AppDbContext _db;
    private readonly TelegramSessionManager _sessionManager;

    public NotifierService(AppDbContext db, TelegramSessionManager sessionManager)
    {
        _db = db;
        _sessionManager = sessionManager;
    }

    public async Task NotifyByPostAsync(long postId)
    {
        var post = await _db.Posts
            .Include(p => p.Channel)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            Console.WriteLine($"[Notifier] Post {postId} not found");
            return;
        }

        if (post.Status != PostStatus.Scheduled)
        {
            Console.WriteLine($"[Notifier] Post {postId} is not in Scheduled state, skipping");
            return;
        }

        try
        {
            var session = await _sessionManager.GetOrRestoreAccountSessionAsync(post.Channel.TelegramAccountId);

            var attachments = post.Attachments.OrderBy(a => a.Order).ToList();

            if (attachments.Count == 0)
            {
                await session.Client.SendMessageAsync(
                    chatId: post.Channel.TelegramChatId,
                    inputMessageContent: new TdApi.InputMessageContent.InputMessageText
                    {
                        Text = new TdApi.FormattedText { Text = post.Text }
                    });
            }
            else if (attachments.Count == 1)
            {
                var attachment = attachments[0];
                var isVideo = attachment.MimeType.StartsWith("video/");

                if (isVideo)
                {
                    await session.Client.SendMessageAsync(
                        chatId: post.Channel.TelegramChatId,
                        inputMessageContent: new TdApi.InputMessageContent.InputMessageVideo
                        {
                            Video = new TdApi.InputVideo
                            {
                                Video = new TdApi.InputFile.InputFileLocal { Path = attachment.StorageKey }
                            },
                            Caption = new TdApi.FormattedText { Text = post.Text }
                        });
                }
                else
                {
                    await session.Client.SendMessageAsync(
                        chatId: post.Channel.TelegramChatId,
                        inputMessageContent: new TdApi.InputMessageContent.InputMessagePhoto
                        {
                            Photo = new TdApi.InputPhoto()
                            {
                                Photo = new TdApi.InputFile.InputFileLocal { Path = attachment.StorageKey }
                            },
                            Caption = new TdApi.FormattedText { Text = post.Text }
                        });
                }
            }
            else
            {
                var inputMessages = attachments.Select((a, i) =>
                {
                    var isVideo = a.MimeType.StartsWith("video/");
                    var caption = i == 0 ? new TdApi.FormattedText { Text = post.Text } : null;

                    return isVideo
                        ? (TdApi.InputMessageContent)new TdApi.InputMessageContent.InputMessageVideo
                        {
                            Video = new TdApi.InputVideo
                            {
                                Video = new TdApi.InputFile.InputFileLocal { Path = a.StorageKey }
                            },
                            Caption = caption
                        }
                        : new TdApi.InputMessageContent.InputMessagePhoto
                        {
                            Photo = new TdApi.InputPhoto()
                            {
                                Photo = new TdApi.InputFile.InputFileLocal { Path = a.StorageKey }
                            },
                            Caption = caption
                        };
                }).ToArray();

                await session.Client.SendMessageAlbumAsync(
                    chatId: post.Channel.TelegramChatId,
                    inputMessageContents: inputMessages);
            }

            post.Status = PostStatus.Sent;
            post.SentAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Notifier] Failed to send post {postId}: {ex}");
            post.Status = PostStatus.Failed;
        }

        await _db.SaveChangesAsync();
    }
}