namespace SocialCreditScoreBot2.Interfaces;

public interface IStorageMethod {
    public Task<bool> Init();
    public Task Close();
    public Task SetScore(ulong id, Score score);
    public Task<Score> GetScore(ulong id);
    public Task<Dictionary<ulong, Score>> GetUsersScores(ulong[] users);
}
