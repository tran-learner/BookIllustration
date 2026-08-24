using BookIllustration_Backend.Data;

namespace BookIllustration_Backend.Services;

public class ProjectService
{
    private readonly AppDbContext _dbContext;

    public ProjectService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
}
