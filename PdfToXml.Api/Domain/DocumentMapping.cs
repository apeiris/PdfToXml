using System;
using System.Text.Json.Serialization;

namespace PdfToXml.Api.Domain {
	enum enmDocumentStatus {
		Uploaded,
		Processed,
		Completed,
		Failed
	}
	public class DocumentMapping {
		[JsonPropertyName("id")]
		public Guid Id { get; set; }
		[JsonPropertyName("xmlMapFileName")]
		public string XmlMapFileName { get; set; } = default!;

		[JsonPropertyName("pdfFileName")]
		public string PdfFileName { get; set; } = default!;

		[JsonPropertyName("RepositoryPath")]
		public string RepositoryPath { get; set; } = default!;

		[JsonPropertyName("uploadedAtUtc")]
		public DateTime UploadedAtUtc { get; set; }

		[JsonPropertyName("status")]
		public string Status { get; set; } = enmDocumentStatus.Uploaded.ToString();
		[JsonPropertyName("isChecked")]
		public bool IsChecked { get; set; } 
	}
}
