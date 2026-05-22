public interface IHistoryAnalyticsService
{
    Task<HistoryAnalyticsResponse?> GetHistoryAnalyticsAsync(string userKey);
}