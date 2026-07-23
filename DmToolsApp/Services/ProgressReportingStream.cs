namespace DmToolsApp.Services
{
    /// <summary>
    /// Enveloppe un flux en lecture seule et notifie le nombre cumulé d'octets lus à chaque appel de
    /// Read/ReadAsync. Utilisé pour estimer une progression/ETA sur des API qui consomment un Stream
    /// sans jamais exposer elles-mêmes de retour de progression (ex. FileSaver.SaveAsync du plugin
    /// CommunityToolkit.Maui.Storage) : puisque ces API doivent bien lire le flux qu'on leur donne
    /// pour écrire la destination, intercepter Read() est le seul point d'observation disponible.
    /// </summary>
    public sealed class ProgressReportingStream : Stream
    {
        private readonly Stream _inner;
        private readonly Action<long> _onBytesRead;
        private long _totalRead;

        public ProgressReportingStream(Stream inner, Action<long> onBytesRead)
        {
            _inner = inner;
            _onBytesRead = onBytesRead;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            if (read > 0)
            {
                _totalRead += read;
                _onBytesRead(_totalRead);
            }
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
            if (read > 0)
            {
                _totalRead += read;
                _onBytesRead(_totalRead);
            }
            return read;
        }

        public override void Flush() => _inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
