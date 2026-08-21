namespace Ordo.Services.Shared
{
    public partial class SharedService
    {
        OrdoDbContext _dbContext;

        public SharedService(OrdoDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
