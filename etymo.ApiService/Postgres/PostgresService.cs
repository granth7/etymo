using Dapper;
using Newtonsoft.Json;
using Npgsql;
using Shared.Models;
using System.Data;

namespace etymo.ApiService.Postgres
{
    public class PostgresService(NpgsqlConnection connection)
    {
        private readonly NpgsqlConnection _connection = connection;
        
        public async Task<IEnumerable<WordListOverview>> SelectWordListOverviewsByUserIdAsync(Guid userId)
        {
            string sql = $"SELECT * FROM word_list_overview WHERE creatorguid = '{userId}'";
            return await _connection.QueryAsync<WordListOverview>(sql);
        }
         
        public async Task<int> InsertWordListOverviewAsync(WordListOverview wordListOverview)
        {
            var sql = @"
                INSERT INTO word_list_overview (
                    Guid, CreatorGuid, WordListGuid, IsPublic, Upvotes, 
                    Title, Description, Tags, WordSample, CreatedDate, LastModifiedDate
                )
                VALUES (
                    @Guid, @CreatorGuid, @WordListGuid, @IsPublic, @Upvotes, 
                    @Title, @Description, @Tags, @WordSample::jsonb, @CreatedDate, @LastModifiedDate
                )
                ON CONFLICT (Guid) 
                DO UPDATE SET
                    IsPublic = EXCLUDED.IsPublic,
                    Upvotes = EXCLUDED.Upvotes,
                    Title = EXCLUDED.Title,
                    Description = EXCLUDED.Description,
                    Tags = EXCLUDED.Tags,
                    WordSample = EXCLUDED.WordSample::jsonb,
                    LastModifiedDate = EXCLUDED.LastModifiedDate;";

            int rowsAffected = await _connection.ExecuteAsync(sql, wordListOverview);
            return rowsAffected;
        }

        public async Task<int> InsertWordListAsync(WordList wordList)
        {
            var sql = @"
                INSERT INTO word_list (Guid, CreatorGuid, Words)
                VALUES (@Guid, @CreatorGuid, @Words::jsonb)
                ON CONFLICT (Guid)
                DO UPDATE SET
                    Words = EXCLUDED.Words::jsonb;";

            int rowsAffected = await _connection.ExecuteAsync(sql, wordList);
            return rowsAffected;
        }

        public async Task<bool> DeleteWordListOverviewByGuidAsync(Guid wordListOverviewGuid)
        {
            var sql = @"
                DELETE FROM word_list_overview
                WHERE Guid = @Guid;";

            int rowsAffected = await _connection.ExecuteAsync(sql, new { Guid = wordListOverviewGuid });
            return rowsAffected > 0; // Returns true if a row was deleted
        }
    }
}