using System.Data.SQLite;
using SocialCreditScoreBot2.Interfaces;

namespace SocialCreditScoreBot2.Storage;

public class SqliteStorage : IStorageMethod {
    private const string ConnectionString = "Data Source=";
    private SQLiteConnection connection;
    private string? version;
    public string Version {
        get {
            if (version == null) {
                FetchVersion();
            }
            return version!;
        }
    }
    
    public async Task<bool> Init() {
        connection = new SQLiteConnection(ConnectionString + "scores.db;");
        await connection.OpenAsync();
        await CreateTables();
        return true;
    }
    
    private void FetchVersion() {
        using SQLiteCommand cmd = new("SELECT SQLITE_VERSION();", connection);
        version = cmd.ExecuteScalar()!.ToString()!;
    }
    
    private async Task CreateTables() {
        SQLiteCommand cmd = new(@"
CREATE TABLE IF NOT EXISTS scores (
    id BIGINT UNSIGNED PRIMARY KEY, 
    total DOUBLE, 
    sentences INT UNSIGNED,
    worst_score_text TEXT,
    worst_score_value DOUBLE,
    best_score_text TEXT,
    best_score_value DOUBLE
);
", connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task Close() {
        await connection.CloseAsync();
    }

    /// <summary>
    /// Sets the score in the database. If the user is not found, it will create a new entry.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="score"></param>
    public async Task SetScore(ulong id, Score score) {
        using SQLiteCommand cmd = new(@"
            INSERT INTO scores (id, total, sentences, worst_score_text, worst_score_value, best_score_text, best_score_value)
            VALUES (@id, @total, @sentences, @worst_score_text, @worst_score_value, @best_score_text, @best_score_value)
            ON CONFLICT(id) DO UPDATE SET
                total = @total,
                sentences = @sentences,
                worst_score_text = @worst_score_text,
                worst_score_value = @worst_score_value,
                best_score_text = @best_score_text,
                best_score_value = @best_score_value;
        ", connection);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@total", score.Total);
        cmd.Parameters.AddWithValue("@sentences", score.Sentences);
        cmd.Parameters.AddWithValue("@worst_score_text", score.WorstScoreText);
        cmd.Parameters.AddWithValue("@worst_score_value", score.WorstScoreValue);
        cmd.Parameters.AddWithValue("@best_score_text", score.BestScoreText);
        cmd.Parameters.AddWithValue("@best_score_value", score.BestScoreValue);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the score from the database and returns new Score() if the user is not found.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Score> GetScore(ulong id) {
        using SQLiteCommand cmd = new("SELECT * FROM scores WHERE id = @id;", connection);
        cmd.Parameters.AddWithValue("@id", id);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) {
            return new Score();
        }
        return new Score {
            Total = reader.GetDouble(1),
            Sentences = (uint) reader.GetInt32(2),
            WorstScoreText = reader.GetString(3),
            WorstScoreValue = reader.GetDouble(4),
            BestScoreText = reader.GetString(5),
            BestScoreValue = reader.GetDouble(6)
        };
    }

    /// <summary>
    /// Gets the scores of multiple users from the database.
    /// </summary>
    /// <param name="users">An array of user ids to look for.</param>
    /// <returns>A dictionary mapping the user id to their score. The mapping will be a new Score() if that user isn't there.</returns>
    public async Task<Dictionary<ulong, Score>> GetUsersScores(ulong[] users) {
        Dictionary<ulong, Score> result = new();
        using SQLiteCommand cmd = new("SELECT * FROM scores WHERE id IN (" + string.Join(", ", users) + ");", connection);
        using SQLiteDataReader reader = cmd.ExecuteReader();
        while (reader.Read()) {
            ulong id = (ulong) reader.GetInt64(0);
            result[id] = new Score {
                Total = reader.GetDouble(1),
                Sentences = (uint) reader.GetInt32(2),
                WorstScoreText = reader.GetString(3),
                WorstScoreValue = reader.GetDouble(4),
                BestScoreText = reader.GetString(5),
                BestScoreValue = reader.GetDouble(6)
            };
        }
        return result;
    }
}