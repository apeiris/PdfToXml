using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using PdfToXml.Api.Data;
using PdfToXml.Api.Domain;
using SpatialPdfParser;

namespace PdfToXml.Api.Controllers;



[ApiController]
[Route("api/document-mappings")]
public class DocumentMappingsController : ControllerBase {

	private readonly DocumentMappingRepository _repository;

	public DocumentMappingsController(DocumentMappingRepository repository) {
		_repository = repository;
	}

	// GET: api/document-mappings
	[HttpGet]
	public async Task<ActionResult<List<DocumentMapping>>> GetAll() {
		var mappings = await _repository.GetAllAsync();

		var result = mappings.Select(m => new DocumentMapping {
			Id = m.Id,
			XmlMapFileName = m.XmlMapFileName,

			PdfFileName = m.PdfFileName,
			RepositoryPath = m.RepositoryPath,
			UploadedAtUtc = m.UploadedAtUtc,
			Status = m.Status
		}).ToList();

		return Ok(result);
	}

	// GET: api/document-mappings/xml/{id}
	[HttpGet("xml/{id}")]
	public async Task<IActionResult> GetXml(Guid id) {
		var mapping = await _repository.GetByIdAsync(id);



		if (mapping == null)
			return NotFound();
		var xmlPath = Path.Combine(mapping.RepositoryPath, mapping.XmlMapFileName);
		if (!System.IO.File.Exists(xmlPath))
			return NotFound("XML file not found.");

		return PhysicalFile(xmlPath, "application/xml");
	}

	// GET: api/document-mappings/pdf/{id}
	[HttpGet("pdf/{id}")]
	public async Task<IActionResult> GetPdf(Guid id) {
		var mapping = await _repository.GetByIdAsync(id);
		if (mapping == null)
			return NotFound();

		var pdfPath = Path.Combine(mapping.RepositoryPath, mapping.PdfFileName);
		if (!System.IO.File.Exists(pdfPath))
			return NotFound("PDF file not found.");

		return PhysicalFile(pdfPath, "application/pdf");
	}

	[HttpPost("{id}/parse")]
	public async Task Parse(Guid id) {
		Response.ContentType = "application/json";

		var mapping = await _repository.GetByIdAsync(id);
		if (mapping == null) {
			Response.StatusCode = 404;
			await Response.WriteAsync(JsonSerializer.Serialize(
				new { type = "error", message = "Document mapping not found." }));
			return;
		}

		await using var writer = new StreamWriter(Response.Body);
		var channel = Channel.CreateUnbounded<string>();

		var progress = new Progress<(int percent, string message, XmlMapProcessor.enmLevelReturns)>(p => {
			var json = JsonSerializer.Serialize(new {
				type = "progress",
				percent = p.percent,
				message = p.message,
				XmlMapProcessor = XmlMapProcessor.enmLevelReturns.Info.ToString()
			});

			channel.Writer.TryWrite(json);
		});

		var processor = new XmlMapProcessor();

		var processingTask = Task.Run(async () => {
			try {
				var xmlMapPath = Path.Combine(
					mapping.RepositoryPath,
					mapping.XmlMapFileName
				);

				XElement result = await processor.ProcessPdfAndMapAsync(
					xmlMapPath,
					mapping.RepositoryPath,
					progress
				);

				channel.Writer.TryWrite(JsonSerializer.Serialize(new {
					type = "result",
					xml = result.ToString()
				}));
			} catch (Exception ex) {
				channel.Writer.TryWrite(JsonSerializer.Serialize(new {
					type = "error",
					message = ex.Message
				}));
			}
			finally {
				channel.Writer.Complete();
			}
		});

		await foreach (var msg in channel.Reader.ReadAllAsync()) {
			await writer.WriteLineAsync(msg);
			await writer.FlushAsync();
		}

		await processingTask;
	}
	class ParseMessage {
		public string? Type { get; set; }       // "progress" or "result"
		public int? Percent { get; set; }       // 0–100
		public string? Message { get; set; }    // status message
		public string? Xml { get; set; }        // result XML
	}

	private static readonly ConcurrentDictionary<Guid, ParseMessage> _progressStore = new();
	[HttpPost("{id}/parse-start")]
	public IActionResult StartParse(Guid id) {
		if (_progressStore.TryGetValue(id, out var state)	&& state.Type == "progress") {
			return Conflict("Parse already in progress");
		}

		// initial state
		_progressStore[id] = new ParseMessage {
			Type = "progress",
			Percent = 0,
			Message = "Starting…"
		};

		_ = Task.Run(async () => {
			try {
				var mapping = await _repository.GetByIdAsync(id);
				if (mapping == null)
					throw new Exception("Document mapping not found");

				var processor = new XmlMapProcessor();

				XElement result = await processor.ProcessPdfAndMapAsync(
					mapping.XmlMapFileName,
					mapping.RepositoryPath,
					new Progress<(int percent, string message, XmlMapProcessor.enmLevelReturns)>(p => {
						_progressStore[id] = new ParseMessage {
							Type = "progress",
							Percent = p.percent,
							Message = p.message
						};
					})
				);

				// ✅ finished successfully
				_progressStore[id] = new ParseMessage {
					Type = "result",
					Percent = 100,
					Message = "Completed",
					Xml = result.ToString()
				};
			} catch (Exception ex) {
				// ✅ failure still returns result
				_progressStore[id] = new ParseMessage {
					Type = "result",
					Percent = 100,
					Message = "Failed",
					Xml = ex.ToString()
				};
			}

			// 🧹 auto-clean after 30 seconds
			_ = Task.Delay(TimeSpan.FromSeconds(1))
				.ContinueWith(_ => _progressStore.TryRemove(id, out ParseMessage _));

		});
		

		return Ok();
	}


	[HttpGet("{id}/progress")]
	public IActionResult GetProgress(Guid id) {
		if (_progressStore.TryGetValue(id, out var progress))
			return Ok(progress);

		return NotFound();
	}

	[HttpPost("{id}/parse-clear")]
	public IActionResult ClearParse(Guid id) {
		_progressStore.TryRemove(id, out _);
		return Ok();
	}

	[HttpPost("{id}/delete")]
	public async Task<IActionResult> Delete(Guid id) {
		var mapping = await _repository.GetByIdAsync(id);
		if (mapping == null)
			return NotFound();

		_repository.DeleteAsync(id);
		// Delete files from disk
		if (Directory.Exists(mapping.RepositoryPath)) {
			Directory.Delete(mapping.RepositoryPath, true);


		}
		// OPTIONAL: Implement deletion from database if needed
		return NoContent();
	}

	//-----------

}
