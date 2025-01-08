using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace etymo.ApiService
{
    public class PostgresService(NpgsqlConnection connection)
    {
        private readonly NpgsqlConnection _connection = connection;

        public async Task<IEnumerable<MyEntity>> GetEntitiesAsync()
        {
            var result = new List<MyEntity>();
            await _connection.OpenAsync();
            using (var cmd = new NpgsqlCommand("SELECT * FROM my_table", _connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new MyEntity
                    {
                        // Map your columns to your entity properties here
                        Id = reader.GetInt32(0), // Assuming 'id' is the first column
                        Name = reader.GetString(1), // Assuming 'name' is the second column
                        CreatedDate = reader.GetDateTime(2) // Assuming 'created_date' is the third column
                    });
                }
            }
            await _connection.CloseAsync();
            return result;
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class PostgresController : ControllerBase
    {
        private readonly PostgresService _PostgresService;

        public PostgresController(PostgresService PostgresService)
        {
            _PostgresService = PostgresService;
        }

        [HttpGet]
        public async Task<IEnumerable<MyEntity>> Get()
        {
            return await _PostgresService.GetEntitiesAsync();
        }
    }

}
