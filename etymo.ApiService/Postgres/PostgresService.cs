using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Shared.Models;
using System.Data;

namespace etymo.ApiService.Postgres
{
    public class PostgresService(NpgsqlConnection connection)
    {
        private readonly NpgsqlConnection _connection = connection;
        
        public async Task<WordListOverview> FetchWordListOverviewAsync(Guid wordListOverviewId)
        {
            string sql = $"SELECT * FROM word_list_overview WHERE guid='{wordListOverviewId}' AND ispublic = true";

            return await _connection.QuerySingleAsync<WordListOverview>(sql);
        }

        public async Task<WordListOverview> FetchPrivateWordListOverviewAsync(Guid wordListOverviewId, Guid userId)
        {
            string sql = $"SELECT * FROM word_list_overview WHERE guid='{wordListOverviewId}' AND creatorguid = '{userId}' AND ispublic = false";

            return await _connection.QuerySingleAsync<WordListOverview>(sql);
        }

        public async Task<WordList?> FetchWordListAsync(Guid wordListId)
        {
            string sql = $"SELECT * FROM word_list WHERE guid='{wordListId}' AND ispublic = true";

            return await _connection.QuerySingleOrDefaultAsync<WordList>(sql);
        }

        public async Task<WordList> FetchPrivateWordListAsync(Guid wordListId, Guid userId)
        {
            string sql = $"SELECT * FROM word_list WHERE guid='{wordListId}' AND creatorguid = '{userId}' AND ispublic = false";

            return await _connection.QuerySingleAsync<WordList>(sql);
        }

        public async Task<List<WordListOverview>> SelectPublicWordListOverviewsAsync(
            Guid? userId = null,
            DateRange? dateRange = null,
            int pageNumber = 1,
            int pageSize = 10,
            Guid? currentUserGuid = null,
            string? tagSearch = null)  // Add parameter for tag search
        {
            string sql = "SELECT * FROM word_list_overview WHERE 1=1";

            // Add filters only if they're provided
            if (userId != null)
            {
                sql += $" AND creatorguid = '{userId}'";
            }

            if (dateRange.HasValue)
            {
                // Handle date range filter based on the enum value
                switch (dateRange.Value)
                {
                    case DateRange.Today:
                        sql += " AND createddate::date = CURRENT_DATE";
                        break;
                    case DateRange.Week:
                        sql += " AND createddate >= CURRENT_DATE - INTERVAL '7 days'";
                        break;
                    case DateRange.Month:
                        sql += " AND createddate >= CURRENT_DATE - INTERVAL '1 month'";
                        break;
                }
            }

            // Add tag search if provided
            if (!string.IsNullOrWhiteSpace(tagSearch))
            {
                // Search for tags that contain the search term
                sql += " AND EXISTS (SELECT 1 FROM unnest(tags) AS tag WHERE LOWER(tag) LIKE LOWER(@TagSearch))";
            }

            sql += " AND ispublic = true";

            // Add ORDER BY clause to sort by upvotes in descending order
            sql += " ORDER BY upvotes DESC";

            // Add pagination
            sql += $" LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize}";

            var parameters = new DynamicParameters();
            if (!string.IsNullOrWhiteSpace(tagSearch))
            {
                parameters.Add("TagSearch", $"%{tagSearch}%");
            }

            var wordListOverviews = (List<WordListOverview>)await _connection.QueryAsync<WordListOverview>(sql, parameters);

            // If we have a current user, fetch their upvote status for these word lists
            if (currentUserGuid.HasValue && currentUserGuid != Guid.Empty && wordListOverviews.Count != 0)
            {
                var wordListOverviewGuids = wordListOverviews.Select(wl => wl.Guid).ToArray();
                string upvoteSql = "SELECT word_list_overview_guid FROM user_upvotes WHERE user_guid = @UserGuid AND word_list_overview_guid = ANY(@WordListGuids)";
                var upvotedLists = await _connection.QueryAsync<Guid>(upvoteSql, new { UserGuid = currentUserGuid, WordListGuids = wordListOverviewGuids });

                // Create a dictionary of word list GUIDs to upvote status
                var upvoteSet = new HashSet<Guid>(upvotedLists);

                // Enrich the word list objects with upvote status
                foreach (var wordListOverview in wordListOverviews)
                {
                    wordListOverview.UserHasUpvoted = upvoteSet.Contains(wordListOverview.Guid);
                }
            }

            return wordListOverviews;
        }

        public async Task<List<WordListOverview>> SelectPrivateWordListOverviewsAsync(
            Guid userId,
            DateRange? dateRange = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            string sql = $"SELECT * FROM word_list_overview WHERE creatorguid = '{userId}'";

            if (dateRange.HasValue)
            {
                // Handle date range filter based on the enum value if provided
                switch (dateRange.Value)
                {
                    case DateRange.Today:
                        sql += " AND createddate::date = CURRENT_DATE";
                        break;
                    case DateRange.Week:
                        sql += " AND createddate >= CURRENT_DATE - INTERVAL '7 days'";
                        break;
                    case DateRange.Month:
                        sql += " AND createddate >= CURRENT_DATE - INTERVAL '1 month'";
                        break;
                }
            }

            sql += " AND ispublic = false";

            // Add ORDER BY clause to sort by last modified date in descending order
            sql += " ORDER BY createddate DESC";

            // Add pagination
            sql += $" LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize}";

            return (List<WordListOverview>)await _connection.QueryAsync<WordListOverview>(sql);
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
                INSERT INTO word_list (Guid, CreatorGuid, IsPublic, Words)
                VALUES (@Guid, @CreatorGuid, @IsPublic, @Words::jsonb)
                ON CONFLICT (Guid)
                DO UPDATE SET
                    IsPublic = EXCLUDED.IsPublic,
                    Words = EXCLUDED.Words::jsonb;";

            int rowsAffected = await _connection.ExecuteAsync(sql, wordList);
            return rowsAffected;
        }

        public async Task<bool> DeleteWordListOverviewByGuidAsync(Guid wordlistOverviewId, Guid userId)
        {
            var sql = @"
                DELETE FROM word_list_overview
                WHERE Guid = @Guid
                AND CreatorGuid = @CreatorGuid;";

            int rowsAffected = await _connection.ExecuteAsync(sql, new { Guid = wordlistOverviewId, CreatorGuid = userId });
            return rowsAffected > 0; // Returns true if a row was deleted
        }

        public async Task<bool> ToggleUpvoteAsync(Guid userGuid, Guid wordListOverviewGuid)
        {
            // Ensure the connection is open before trying to begin a transaction
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }

            using var transaction = _connection.BeginTransaction();

            try
            {
                var existingUpvote = await _connection.QueryFirstOrDefaultAsync<UserUpvote>(
                    "SELECT * FROM user_upvotes WHERE user_guid = @UserGuid AND word_list_overview_guid = @WordListGuid",
                    new { UserGuid = userGuid, WordListGuid = wordListOverviewGuid },
                    transaction
                );

                bool isUpvoted;

                if (existingUpvote != null)
                {
                    await _connection.ExecuteAsync(
                        "DELETE FROM user_upvotes WHERE user_guid = @UserGuid AND word_list_overview_guid = @WordListGuid",
                        new { UserGuid = userGuid, WordListGuid = wordListOverviewGuid },
                        transaction
                    );

                    await _connection.ExecuteAsync(
                        "UPDATE word_list_overview SET upvotes = upvotes - 1 WHERE guid = @WordListGuid",
                        new { WordListGuid = wordListOverviewGuid },
                        transaction
                    );

                    isUpvoted = false;
                }
                else
                {
                    await _connection.ExecuteAsync(
                        "INSERT INTO user_upvotes (user_guid, word_list_overview_guid) VALUES (@UserGuid, @WordListGuid)",
                        new { UserGuid = userGuid, WordListGuid = wordListOverviewGuid },
                        transaction
                    );

                    await _connection.ExecuteAsync(
                        "UPDATE word_list_overview SET upvotes = upvotes + 1 WHERE guid = @WordListGuid",
                        new { WordListGuid = wordListOverviewGuid },
                        transaction
                    );

                    isUpvoted = true;
                }

                transaction.Commit();
                return isUpvoted;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> GetUpvoteCountAsync(Guid wordListOverviewGuid)
        {
            return await _connection.ExecuteScalarAsync<int>(
                "SELECT upvotes FROM word_list_overview WHERE guid = @WordListGuid",
                new { WordListGuid = wordListOverviewGuid }
            );
        }

        public async Task<Dictionary<Guid, bool>> GetUserUpvoteStatusForListsAsync(Guid userGuid, IEnumerable<Guid> wordListGuids)
        {
            if (userGuid == Guid.Empty || !wordListGuids.Any())
                return [];

            var upvotedLists = await _connection.QueryAsync<Guid>(
                "SELECT word_list_overview_guid FROM user_upvotes WHERE user_guid = @UserGuid AND word_list_overview_guid = ANY(@WordListGuids)",
                new { UserGuid = userGuid, WordListGuids = wordListGuids.ToArray() }
            );

            return wordListGuids.ToDictionary(guid => guid, guid => upvotedLists.Contains(guid));
        }

        public async Task<int> CreateReport(ReportRequest request, string userId)
        {
            var sql = @"
                INSERT INTO reports (reportedcontentid, reporteruserid, reason, details)
                VALUES (@ContentId::uuid, @UserId, @Reason, @Details)
                RETURNING id";
            var id = await _connection.ExecuteScalarAsync<int>(sql, new
            {
                request.ContentId,
                UserId = userId,
                request.Reason,
                request.Details
            });
            return id;
        }

        public async Task<List<Report>> GetReports()
        {
            var sql = @"
                SELECT r.*, wl.Title as ContentTitle, wl.Description as ContentDescription, wl.Tags as ContentTags, wl.CreatorGuid as ReportedUserId, wl.WordListGuid as ContentWordsId
                FROM reports r
                JOIN word_list_overview wl ON r.reportedcontentid = wl.Guid
                WHERE r.status = 'pending'
                ORDER BY r.createdat DESC";
            return [.. (await _connection.QueryAsync<Report>(sql))];
        }

        public async Task<int> ResolveReports(int reportId, string action, string resolverId)
        {
            // Update report status
            await _connection.ExecuteAsync(
                "UPDATE reports SET status = @Status, resolvedat = NOW(), resolveruserid = @UserId WHERE id = @Id",
                new { Status = action, UserId = resolverId, Id = reportId });

            // If action is "remove", also hide the content
            if (action == "remove")
            {
                // Get content ID (GUID corresponding to word list overview) from reportId (int)
                var contentId = await _connection.ExecuteScalarAsync<Guid>(
                    "SELECT ReportedContentId FROM reports WHERE id = @Id",
                    new { Id = reportId });

                // Update content status in word_list_overview
                return await _connection.ExecuteAsync(
                    "UPDATE word_list_overview SET ishidden = true WHERE Guid = @Id::uuid",
                    new { Id = contentId });
            }
            else
            {
                return 0;
            }
        }
    }
}