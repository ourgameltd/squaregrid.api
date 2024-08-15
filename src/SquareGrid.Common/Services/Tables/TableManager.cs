using Azure;
using Azure.Data.Tables;

public class TableManager
{
    private readonly Dictionary<string, TableClient> tableClientDictionary;

    public TableManager(Dictionary<string, TableClient> tableClientDictionary)
    {
        this.tableClientDictionary = tableClientDictionary;
    }

    public async Task Insert<T>(T entity) where T : class, ITableEntity
    {
        var tableClient = tableClientDictionary[typeof(T).Name];
        await tableClient.AddEntityAsync(entity);
    }

    public async Task Update<T>(T entity) where T : class, ITableEntity
    {
        var tableClient = tableClientDictionary[typeof(T).Name];
        await tableClient.UpdateEntityAsync(entity, entity.ETag, mode: TableUpdateMode.Replace);
    }

    public async Task DeleteAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity
    {
        var tableClient = tableClientDictionary[typeof(T).Name];
        await tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    public async Task<T?> GetAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity
    {
        try
        {
            var tableClient = tableClientDictionary[typeof(T).Name];
            var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync<T>(string partitionKey) where T : class, ITableEntity
    {
        var tableClient = tableClientDictionary[typeof(T).Name];
        var entities = new List<T>();
        await foreach (var entity in tableClient.QueryAsync<T>(filter: $"PartitionKey eq '{partitionKey}'"))
        {
            entities.Add(entity);
        }
        return entities;
    }
}
