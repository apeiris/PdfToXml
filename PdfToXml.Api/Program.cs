
using Dapper;
using Microsoft.Data.Sqlite;
using PdfToXml.Api.Data;




SqlMapper.AddTypeHandler(new GuidTypeHandler());

static void EnsureDatabase(string connectionString) {
	using var connection = new SqliteConnection(connectionString);

	connection.Execute(@"
        CREATE TABLE IF NOT EXISTS DocumentMappings (
            Id TEXT PRIMARY KEY,
            XmlMapFileName TEXT,
            PdfFileName TEXT,
            RepositoryPath TEXT,
            UploadedAtUtc TEXT,
            Status TEXT
        );
    ");
}


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
	options.AddPolicy("AllowBlazor",
		policy => policy
			.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader());
});

builder.Services.AddScoped<DocumentMappingRepository>();

builder.Services.AddControllers();


//builder.Services.AddDbContext<SpatialDataContext>(options =>
//	options.UseSqlServer(builder.Configuration.GetConnectionString("SpatialData")));
var app = builder.Build();

var connString = builder.Configuration.GetConnectionString("SpatialData");
EnsureDatabase(connString); EnsureDatabase(builder.Configuration.GetConnectionString("SpatialData_SL")!);
app.UseCors("AllowBlazor");
app.MapControllers();

app.Run();
