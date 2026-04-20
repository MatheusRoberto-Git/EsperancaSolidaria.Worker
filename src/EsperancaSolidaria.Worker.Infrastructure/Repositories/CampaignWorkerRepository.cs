using Dapper;
using Microsoft.Data.SqlClient;

namespace EsperancaSolidaria.Worker.Infrastructure.Repositories
{
    public class CampaignWorkerRepository : ICampaignWorkerRepository
    {
        private readonly string _connectionString;

        public CampaignWorkerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task UpdateAmountRaised(long campaignId, decimal amount)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync("UPDATE Campaigns SET AmountRaised = AmountRaised + @Amount WHERE Id = @CampaignId AND Active = 1",
                new { Amount = amount,CampaignId = campaignId });
        }
    }
}