// Services/IApplicationService.cs
using Eduflex.API.DTOs;
using Eduflex.API.Models;
using MongoDB.Driver;

public interface IApplicationService
{
    Task<List<ApplicationDto>> GetApplicationsByStudentId(string studentId);
    Task<ApplicationDetailDto> GetApplicationById(string id);
    Task<ApplicationDto> CreateApplication(CreateApplicationDto createDto);
    Task<bool> UpdateApplicationStatus(string id, string status);
}

// Services/ApplicationService.cs
public class ApplicationService : IApplicationService
{
    private readonly IMongoCollection<Application> _applicationsCollection;

    public ApplicationService(IMongoDatabase database)
    {
        _applicationsCollection = database.GetCollection<Application>("Applications");

        // Create index for better query performance
        var indexKeysDefinition = Builders<Application>.IndexKeys.Ascending(a => a.StudentId);
        _applicationsCollection.Indexes.CreateOne(new CreateIndexModel<Application>(indexKeysDefinition));
    }

    public async Task<List<ApplicationDto>> GetApplicationsByStudentId(string studentId)
    {
        var applications = await _applicationsCollection
            .Find(a => a.StudentId == studentId)
            .SortByDescending(a => a.DateApplied)
            .ToListAsync();

        return applications.Select(a => new ApplicationDto
        {
            Id = a.Id,
            Description = a.Description,
            DateApplied = a.DateApplied,
            Status = a.Status,
            ApplicationType = a.ApplicationType
        }).ToList();
    }

    public async Task<ApplicationDetailDto> GetApplicationById(string id)
    {
        var application = await _applicationsCollection
            .Find(a => a.Id == id)
            .FirstOrDefaultAsync();

        if (application == null) return null;

        return new ApplicationDetailDto
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

    public async Task<ApplicationDto> CreateApplication(CreateApplicationDto createDto)
    {
        var application = new Application
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

        return new ApplicationDto
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
        var update = Builders<Application>.Update
            .Set(a => a.Status, status);

        var result = await _applicationsCollection
            .UpdateOneAsync(a => a.Id == id, update);

        return result.ModifiedCount > 0;
    }
}