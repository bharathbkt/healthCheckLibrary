using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace HealthMonitoringModule;

public class OracleSchemaValidationHealthCheck<TDbContext> : IHealthCheck, ITransientDependency
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<OracleSchemaValidationHealthCheck<TDbContext>> _logger;

    public OracleSchemaValidationHealthCheck(TDbContext dbContext, ILogger<OracleSchemaValidationHealthCheck<TDbContext>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var mismatches = new List<string>();
            var model = _dbContext.Model;

            // Group by table to avoid N+1 queries. We query metadata for each table once.
            foreach (var entityType in model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                var schemaName = entityType.GetSchema();

                if (string.IsNullOrEmpty(tableName))
                    continue;

                // Oracle defaults to uppercase representation
                var oracleTableName = tableName.ToUpperInvariant();
                
                // Determine owner/schema filter to support multi-schema environments
                string ownerCondition = string.IsNullOrEmpty(schemaName) 
                    ? "OWNER = USER" 
                    : $"OWNER = '{schemaName.ToUpperInvariant()}'";

                // Querying ALL_TAB_COLUMNS for all columns of this specific table at once
                var query = $@"
                    SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH, CHAR_LENGTH
                    FROM ALL_TAB_COLUMNS
                    WHERE TABLE_NAME = '{oracleTableName}' AND {ownerCondition}";

                var dbColumns = new Dictionary<string, OracleColumnMetadata>(StringComparer.OrdinalIgnoreCase);

                var connection = _dbContext.Database.GetDbConnection();
                var connectionWasClosed = connection.State != ConnectionState.Open;

                if (connectionWasClosed)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = query;

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var colName = reader.GetString(0);
                        dbColumns[colName] = new OracleColumnMetadata
                        {
                            ColumnName = colName,
                            DataType = reader.GetString(1),
                            DataLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                            CharLength = reader.IsDBNull(3) ? null : reader.GetInt32(3)
                        };
                    }
                }
                finally
                {
                    if (connectionWasClosed)
                    {
                        await connection.CloseAsync();
                    }
                }

                if (!dbColumns.Any())
                {
                    var msg = $"Table {oracleTableName} not found in database or lacks permissions (ensure SELECT_CATALOG_ROLE or SELECT on ALL_TAB_COLUMNS).";
                    mismatches.Add(msg);
                    _logger.LogWarning("Schema mismatch: {Message}", msg);
                    continue; // Skip column checks if table is missing or unreadable
                }

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnName(StoreObjectIdentifier.Table(tableName, schemaName));
                    if (string.IsNullOrEmpty(columnName)) continue;
                    
                    var oracleColumnName = columnName.ToUpperInvariant();

                    if (!dbColumns.TryGetValue(oracleColumnName, out var dbColumn))
                    {
                        var msg = $"Column {oracleColumnName} missing in table {oracleTableName}.";
                        mismatches.Add(msg);
                        _logger.LogWarning("Schema mismatch: {Message}", msg);
                        continue;
                    }

                    // Validation Rule 1: Compare Data Length (Max Length)
                    var expectedMaxLength = property.GetMaxLength();
                    var actualLength = dbColumn.CharLength > 0 ? dbColumn.CharLength : dbColumn.DataLength;

                    if (expectedMaxLength.HasValue && actualLength.HasValue && actualLength > 0)
                    {
                        if (expectedMaxLength.Value != actualLength.Value)
                        {
                            var msg = $"Table {oracleTableName} column {oracleColumnName} max length mismatch. EF Expected: {expectedMaxLength.Value}, DB Found: {actualLength.Value}.";
                            mismatches.Add(msg);
                            _logger.LogWarning("Schema mismatch: {Message}", msg);
                        }
                    }

                    // Validation Rule 2: Compare Data Type (basic type resolution)
                    var expectedColumnType = property.GetColumnType();
                    if (!string.IsNullOrEmpty(expectedColumnType))
                    {
                        // E.g., strip 'VARCHAR2(100)' to 'VARCHAR2' to match Base DataType
                        var expectedBaseType = expectedColumnType.Split('(')[0].ToUpperInvariant();
                        var actualBaseType = dbColumn.DataType.ToUpperInvariant();

                        if (expectedBaseType != actualBaseType && !IsCompatibleType(expectedBaseType, actualBaseType))
                        {
                            var msg = $"Table {oracleTableName} column {oracleColumnName} data type mismatch. EF Expected: {expectedBaseType}, DB Found: {actualBaseType}.";
                            mismatches.Add(msg);
                            _logger.LogWarning("Schema mismatch: {Message}", msg);
                        }
                    }
                }
            }

            if (mismatches.Any())
            {
                var dict = new Dictionary<string, object> { { "Mismatches", mismatches } };
                return HealthCheckResult.Degraded("Oracle schema validation found mismatches.", data: dict);
            }

            return HealthCheckResult.Healthy("Oracle schema validation passed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred while executing Oracle schema validation health check.");
            return new HealthCheckResult(context.Registration.FailureStatus, "Oracle schema validation failed due to an exception.", ex);
        }
    }

    private bool IsCompatibleType(string expected, string actual)
    {
        // Add specific Oracle type compatibilities if needed, e.g., NUMBER(1) vs NUMBER
        if (expected == "NUMBER" || expected.StartsWith("NUMBER")) 
            return actual.StartsWith("NUMBER") || actual == "FLOAT";
            
        if (expected == "NVARCHAR2" && actual == "VARCHAR2") return false; // Strict
        
        return false;
    }

    private class OracleColumnMetadata
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public int? DataLength { get; set; }
        public int? CharLength { get; set; }
    }
}
