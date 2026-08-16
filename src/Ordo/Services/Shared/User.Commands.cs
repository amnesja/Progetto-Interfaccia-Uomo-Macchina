using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Ordo.Infrastructure;

namespace Ordo.Services.Shared
{
    public class AddOrUpdateUserCommand
    {
        public Guid? Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
    }

    public class RegisterUserCommand
    {
        public string   Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
    }

    public partial class SharedService
    {
        public async Task<Guid> Handle(AddOrUpdateUserCommand cmd)
        {
            var user = await _dbContext.Users
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                user = new User
                {
                    Email = cmd.Email,
                };
                _dbContext.Users.Add(user);
            }

            user.FirstName = cmd.FirstName;
            user.LastName = cmd.LastName;
            user.NickName = cmd.NickName;

            await _dbContext.SaveChangesAsync();

            return user.Id;
        }

        public async Task<Guid> Handle(RegisterUserCommand cmd)
        {
            var usedEmail = await _dbContext.Users.AnyAsync(x => x.Email == cmd.Email);

            if (usedEmail)
            {
                throw new EmailAlreadyExistException("Esiste già un account registrato con quela email");
            }

            var hashedPassword = Ordo.Infrastructure.PasswordHasher.Hash(cmd.Password);
            var user = new User
            {
                Email = cmd.Email,
                Password = hashedPassword,
                FirstName = cmd.FirstName,
                LastName = cmd.LastName,
                NickName = cmd.NickName,
            };
            _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync();
            return user.Id;
        }
    }
}