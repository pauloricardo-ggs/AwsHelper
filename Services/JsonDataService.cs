using System.Text.Json;
using AwsHelper.Models;

namespace AwsHelper.Services;

public class JsonDataService
{
    private readonly string _dataDirectory;
    private readonly string _parameterStoragesFile;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDataService(IWebHostEnvironment environment)
    {
        _dataDirectory = Path.Combine(environment.ContentRootPath, "data");
        _parameterStoragesFile = Path.Combine(_dataDirectory, "parameter-storages.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        if (!File.Exists(_parameterStoragesFile))
        {
            SaveParameterStorages(new List<ParameterStorage>());
        }
    }

    public async Task<List<ParameterStorage>> GetParameterStoragesAsync()
    {
        try
        {
            if (!File.Exists(_parameterStoragesFile))
            {
                return new List<ParameterStorage>();
            }

            var json = await File.ReadAllTextAsync(_parameterStoragesFile);
            var storages = JsonSerializer.Deserialize<List<ParameterStorage>>(json, _jsonOptions);
            return storages ?? new List<ParameterStorage>();
        }
        catch
        {
            return new List<ParameterStorage>();
        }
    }

    public async Task SaveParameterStoragesAsync(List<ParameterStorage> storages)
    {
        var json = JsonSerializer.Serialize(storages, _jsonOptions);
        await File.WriteAllTextAsync(_parameterStoragesFile, json);
    }

    public async Task<ParameterStorage?> GetParameterStorageByIdAsync(Guid id)
    {
        var storages = await GetParameterStoragesAsync();
        return storages.FirstOrDefault(s => s.Id == id);
    }

    public async Task<ParameterStorage> AddParameterStorageAsync(ParameterStorage storage)
    {
        var storages = await GetParameterStoragesAsync();
        
        // Gerar novo ID
        storage.Id = Guid.NewGuid();
        
        storages.Add(storage);
        await SaveParameterStoragesAsync(storages);
        
        return storage;
    }

    public async Task<bool> UpdateParameterStorageAsync(ParameterStorage storage)
    {
        var storages = await GetParameterStoragesAsync();
        var index = storages.FindIndex(s => s.Id == storage.Id);
        
        if (index == -1)
        {
            return false;
        }

        storages[index] = storage;
        await SaveParameterStoragesAsync(storages);
        
        return true;
    }

    public async Task<bool> DeleteParameterStorageAsync(Guid id)
    {
        var storages = await GetParameterStoragesAsync();
        var storage = storages.FirstOrDefault(s => s.Id == id);
        
        if (storage == null)
        {
            return false;
        }

        storages.Remove(storage);
        await SaveParameterStoragesAsync(storages);
        
        return true;
    }

    private void SaveParameterStorages(List<ParameterStorage> storages)
    {
        var json = JsonSerializer.Serialize(storages, _jsonOptions);
        File.WriteAllText(_parameterStoragesFile, json);
    }
}
