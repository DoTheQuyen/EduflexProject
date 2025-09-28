using ShareService.Models;
using MongoDB.Driver;

public interface IApplicationService
{
    Task<List<ApplicationModel>> GetApplicationsByStudentId(string studentId);
    Task<ApplicationDetailModel> GetApplicationById(string id);
    Task<ApplicationModel> CreateApplication(CreateApplicationModel createDto);
    Task<bool> UpdateApplicationStatus(string id, string status);
}

// Services/ApplicationService.cs
public class ApplicationService : IApplicationService
{
    private readonly IMongoCollection<ApplicationModel> _applicationsCollection;

    public ApplicationService(IMongoDatabase database)
    {
        _applicationsCollection = database.GetCollection<ApplicationModel>("Applications");

        // Create index for better query performance
        var indexKeysDefinition = Builders<ApplicationModel>.IndexKeys.Ascending(a => a.StudentId);
        _applicationsCollection.Indexes.CreateOne(new CreateIndexModel<ApplicationModel>(indexKeysDefinition));
    }

    public async Task<List<ApplicationModel>> GetApplicationsByStudentId(string studentId)
    {
        var applications = await _applicationsCollection
            .Find(a => a.StudentId == studentId)
            .SortByDescending(a => a.DateApplied)
            .ToListAsync();

        return applications.Select(a => new ApplicationModel
        {
            Id = a.Id,
            Description = a.Description,
            DateApplied = a.DateApplied,
            Status = a.Status,
            ApplicationType = a.ApplicationType
        }).ToList();
    }

    public async Task<ApplicationDetailModel> GetApplicationById(string id)
    {
        var application = await _applicationsCollection
            .Find(a => a.Id == id)
            .FirstOrDefaultAsync();

        if (application == null) return null;

        return new ApplicationDetailModel
        {
            Id = application.Id,
            StudentId = application.StudentId,
            StudentName = application.StudentName,
            Description = application.Description,
            DateApplied = application.DateApplied,
            Status = application.Status,
            Details = application.Details,
            ApplicationType = application.ApplicationType
        };
    }

    public async Task<ApplicationModel> CreateApplication(CreateApplicationModel createDto)
    {
        var application = new ApplicationModel
        {
            StudentId = createDto.StudentId,
            StudentName = createDto.StudentName,
            Description = createDto.Description,
            Details = createDto.Details,
            ApplicationType = createDto.ApplicationType,
            DateApplied = DateTime.UtcNow,
            Status = "Pending"
        };

        await _applicationsCollection.InsertOneAsync(application);

        return new ApplicationModel
        {
            Id = application.Id,
            Description = application.Description,
            DateApplied = application.DateApplied,
            Status = application.Status,
            ApplicationType = application.ApplicationType
        };
    }

    public async Task<bool> UpdateApplicationStatus(string id, string status)
    {
        var update = Builders<ApplicationModel>.Update
            .Set(a => a.Status, status);

        var result = await _applicationsCollection
            .UpdateOneAsync(a => a.Id == id, update);

        return result.ModifiedCount > 0;
    }
}