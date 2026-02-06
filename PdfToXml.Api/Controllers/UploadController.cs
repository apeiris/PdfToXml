using Microsoft.AspNetCore.Mvc;

using PdfToXml.Api.Data;  // Your DbContext namespace
using PdfToXml.Api.Domain;
//using PdfToXml.Api.Models; // Your Mapping entity


namespace PdfToXml.Api.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase {
	private readonly IWebHostEnvironment _env;
	private readonly DocumentMappingRepository _repository;
	public UploadController(IWebHostEnvironment env, DocumentMappingRepository repo) {
		_env = env;
		_repository = repo;
	}

	[HttpPost("map-pdf")]
	public async Task<IActionResult> UploadMapAndPdf(List<IFormFile> files) {
		if (files == null || files.Count != 2)	return BadRequest("Exactly one XML and one PDF file are required.");
		var xml = files.FirstOrDefault(f => Path.GetExtension(f.FileName).Equals(".xml", StringComparison.OrdinalIgnoreCase));
		var pdf = files.FirstOrDefault(f => Path.GetExtension(f.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase));
		if (xml == null || pdf == null) return BadRequest("Missing XML or PDF file.");
		var mappingId = Guid.NewGuid().ToString("N");	// Create a logical mapping folder (GUID keeps pairs together)
		var basePath = Path.Combine(_env.ContentRootPath, "Uploads", mappingId);
		Directory.CreateDirectory(basePath);
		var xmlMapPath = Path.Combine(basePath, xml.FileName);
		var pdfPath = Path.Combine(basePath, pdf.FileName);
		await SaveFileAsync(xml, xmlMapPath);
		await SaveFileAsync(pdf, pdfPath);
		var mapping = new DocumentMapping {// ✅ Save mapping to database
			Id = Guid.NewGuid(),
			XmlMapFileName = xml.FileName,
			PdfFileName = pdf.FileName,
			UploadedAtUtc = DateTime.UtcNow,
			RepositoryPath = basePath
		};
		await _repository.InsertAsync(mapping);
		return Ok(new {
			MappingId = mappingId,
			XmlMapFile = xml.FileName,
			PdfFile = pdf.FileName
		});
	}

	private static async Task SaveFileAsync(IFormFile file, string path) {
		await using var stream = new FileStream(path, FileMode.Create);
		await file.CopyToAsync(stream);
	}
}
