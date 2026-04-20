namespace EsperancaSolidaria.Worker.Infrastructure.Repositories
{
    public interface ICampaignWorkerRepository
    {
        Task UpdateAmountRaised(long campaignId, decimal amount);
    }
}