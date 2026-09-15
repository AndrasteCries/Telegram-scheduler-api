namespace TelegramScheduler.DTOs.TelegramAuth;

public record TelegramGetUsersRequest(long UserId);

public record TelegramAccountResponse(
    long Id,
    string? Username,
    string PhoneNumber,
    string? photoPath);