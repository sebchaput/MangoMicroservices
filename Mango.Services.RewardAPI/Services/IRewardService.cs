using Mango.Services.RewardAPI.Message;

namespace Mango.Services.RewardsAPI.Services
{
    public interface IRewardService
    {
        Task UpdateRewards(RewardsMessage rewardsMessage);
    }
}
