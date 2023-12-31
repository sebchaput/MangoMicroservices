using Mango.Services.EmailAPI.Data;
using Mango.Services.EmailAPI.Message;
using Mango.Services.EmailAPI.Models;
using Mango.Services.EmailAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Mango.Services.EmailAPI.Services
{
    public class EmailService : IEmailService
    {
        private DbContextOptions<AppDbContext> _dbOptions;

        public EmailService(DbContextOptions<AppDbContext> dbOptions)
        {
            _dbOptions = dbOptions;
        }

        public async Task EmailCartAndLog(CartDto cartDto)
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine("<br>Cart Email Requested ");
            message.AppendLine("<br>Total " + cartDto.CartHeader.CartTotal.ToString("c"));
            message.AppendLine("<br>");
            message.AppendLine("<ul>");
            foreach (var item in cartDto.CartDetails)
            {
                message.AppendLine("<li>");
                message.AppendLine(item.Product.Name + " : " + item.Count + " x " + item.Product.Price.ToString("c"));
                message.AppendLine("</li>");
            }
            message.AppendLine("</ul>");

            await LogAndEmail(message.ToString(), cartDto.CartHeader.Email);
        }

        public async Task EmailRegisteredUserAndLog(string email)
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine("<br>New User Registered ");
            message.AppendLine("<br>");
            message.AppendLine("<br>Email : " + email);

            await LogAndEmail(message.ToString(), "admin@mangoweb.com");
        }

        public async Task LogOrderPlaced(RewardsMessage rewardsMessage)
        {
            string message = "New Order Placed. <br/> Order ID : " + rewardsMessage.OrderId;
            await LogAndEmail(message, "admin@mangoweb.com");
        }

        private async Task<bool> LogAndEmail(string message, string email)
        {
            try
            {
                EmailLogger emailLogger = new()
                {
                    Email = email,
                    Message = message,
                    EmailSent = DateTime.Now
                };

                await using var _db = new AppDbContext(_dbOptions);
                await _db.EmailLoggers.AddAsync(emailLogger);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
