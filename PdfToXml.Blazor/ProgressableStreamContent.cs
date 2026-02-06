using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public class ProgressableStreamContent : HttpContent {
	private const int DefaultBufferSize = 81920;
	private readonly Stream _stream;
	private readonly Action<long> _progress;

	public ProgressableStreamContent(Stream stream, Action<long> progress) {
		_stream = stream ?? throw new ArgumentNullException(nameof(stream));
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
	}

	protected override async Task SerializeToStreamAsync(Stream target, TransportContext? context) {
		var buffer = new byte[DefaultBufferSize];
		long uploaded = 0;

		int bytesRead;
		while ((bytesRead = await _stream.ReadAsync(buffer)) > 0) {
			await target.WriteAsync(buffer.AsMemory(0, bytesRead));
			uploaded += bytesRead;
		
			_progress(uploaded);
		}
	}

	protected override bool TryComputeLength(out long length) {
		length = _stream.Length;
		return true;
	}
}
