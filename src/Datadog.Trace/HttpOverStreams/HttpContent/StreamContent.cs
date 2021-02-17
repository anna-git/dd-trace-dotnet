using System;
using System.IO;
using System.Threading.Tasks;

namespace Datadog.Trace.HttpOverStreams.HttpContent
{
    internal class StreamContent : IHttpContent
    {
        public StreamContent(Stream stream, long? length)
        {
            Stream = stream;
            Length = length;
        }

        public Stream Stream { get; }

        public long? Length { get; }

        public Task CopyToAsync(Stream destination, int? bufferSize)
        {
            if (bufferSize == null)
            {
                return Stream.CopyToAsync(destination);
            }

            return Stream.CopyToAsync(destination, bufferSize.Value);
        }

        public async Task CopyToAsync(byte[] buffer)
        {
            if (!Length.HasValue)
            {
                throw new InvalidOperationException("Unable to CopyToAsync with buffer when Length is unknown");
            }

            var length = 0;
            var remaining = Length.Value;
            while (true)
            {
                var bytesToRead = (int)Math.Min(remaining, int.MaxValue);
                var bytesRead = await Stream.ReadAsync(buffer, offset: length, count: bytesToRead);

                length += bytesRead;
                remaining -= bytesRead;

                if (bytesRead == 0 || remaining <= 0)
                {
                    return;
                }
            }
        }
    }
}
