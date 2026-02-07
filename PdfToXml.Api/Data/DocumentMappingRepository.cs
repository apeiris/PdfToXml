using Dapper;
using Microsoft.Data.Sqlite;
using PdfToXml.Api.Domain;
namespace PdfToXml.Api.Data;

public class DocumentMappingRepository {
	private readonly string _connectionString;

	public DocumentMappingRepository(IConfiguration configuration) {
		_connectionString = configuration.GetConnectionString("SpatialData_SL")!;
	}

	public async Task<IEnumerable<DocumentMapping>> GetAllAsync() {
		using var conn = new SqliteConnection(_connectionString);
		return await conn.QueryAsync<DocumentMapping>(
			"""
            SELECT *
            FROM DocumentMappings
            ORDER BY UploadedAtUtc DESC
            """);
	}

	public async Task<DocumentMapping?> GetByIdAsync(Guid id) {
		using var conn = new SqliteConnection(_connectionString);
		return await conn.QuerySingleOrDefaultAsync<DocumentMapping>(
			"SELECT * FROM DocumentMappings WHERE Id = @id",
			new { id });
	}

	public async Task InsertAsync(DocumentMapping doc) {
		using var conn = new SqliteConnection(_connectionString);
		await conn.ExecuteAsync(
			"""
            INSERT INTO DocumentMappings
            (Id, XmlMapFileName, PdfFileName, RepositoryPath, UploadedAtUtc, Status)
            VALUES
            (@Id, @XmlMapFileName, @PdfFileName, @RepositoryPath, @UploadedAtUtc, @Status)
            """,
			doc);
	}

	public async Task UpdateStatusAsync(Guid id, string status) {
		using var conn = new SqliteConnection(_connectionString);
		await conn.ExecuteAsync(
			"UPDATE DocumentMappings SET Status = @status WHERE Id = @id",
			new { id, status });
	}

	public async Task DeleteAsync(Guid id) {			
		using var conn = new SqliteConnection(_connectionString);
		await conn.ExecuteAsync(
			"DELETE FROM DocumentMappings WHERE Id = @id",
			new { id });
	}
}
