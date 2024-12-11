using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PinionCore.Remote;

namespace PinionCore.Network
{
    public class _PackageReader
    {
        private readonly IStreamable _Stream;
        private readonly PinionCore.Memorys.IPool _Pool;


        int _Count;
        public _PackageReader(IStreamable stream, PinionCore.Memorys.IPool pool)
        {
            _Stream = stream;
            _Pool = pool;
        }
        public async Task<List<PinionCore.Memorys.Buffer>> Read()
        {


            //System.Console.WriteLine($"read threads {currentCount}");
            System.Threading.Interlocked.Increment(ref _Count);
            var headByte = new byte[1];
            var headBuffer = new System.Collections.Generic.List<byte>();
            do
            {

                var readed = await _Stream.Receive(headByte, 0, 1);
                if (readed == 0)
                    return new List<PinionCore.Memorys.Buffer>();
                headBuffer.Add(headByte[0]);
            } while (headByte[0] > PinionCore.Serialization.Varint.Endian);

            PinionCore.Serialization.Varint.BufferToNumber(headBuffer.ToArray(), 0, out int bodySize);
            var body = new byte[bodySize];
            var offset = 0;
            while (offset < bodySize)
            {

                var readed = await _Stream.Receive(body, offset, bodySize - offset);
                if (readed == 0)
                    return new List<PinionCore.Memorys.Buffer>();
                offset += readed;
            }
            Memorys.Buffer buffer = _Pool.Alloc(bodySize);
            for (var i = 0; i < bodySize; i++)
            {
                buffer[i] = body[i];
            }



            return new List<PinionCore.Memorys.Buffer> { buffer };


        }
    }
    public class PackageReader
    {
        private readonly IStreamable _Stream;
        private readonly PinionCore.Memorys.IPool _Pool;
        private readonly Memorys.Buffer _Empty;

        public struct Package
        {
            public PinionCore.Memorys.Buffer Buffer;
            public ArraySegment<byte> Segment;
        }

        public PackageReader(IStreamable stream, PinionCore.Memorys.IPool pool)
        {

            _Stream = stream;
            _Pool = pool;
            _Empty = _Pool.Alloc(0);
        }

        public Task<List<PinionCore.Memorys.Buffer>> Read()
        {
            return ReadBuffers(new Package { Buffer = _Empty, Segment = _Empty.Bytes });
        }
        public async Task<List<PinionCore.Memorys.Buffer>> ReadBuffers(Package unfin)
        {
            var packages = new List<Package>();
            Memorys.Buffer headBuffer = _Pool.Alloc(8);
            _CopyBuffer(unfin.Segment, headBuffer.Bytes, unfin.Segment.Count);
            var headReadCount = await _ReadFromStream(headBuffer.Bytes.Array, offset: headBuffer.Bytes.Offset + unfin.Segment.Count, count: headBuffer.Bytes.Count - unfin.Segment.Count);
            if (headReadCount == 0)
                return new List<PinionCore.Memorys.Buffer>();
            headReadCount += unfin.Segment.Count;
            var readedSeg = new ArraySegment<byte>(headBuffer.Bytes.Array, headBuffer.Bytes.Offset, headReadCount);
            var varintPackagesLen = 0;
            Serialization.Varint.Package[] varintPackages = PinionCore.Serialization.Varint.FindPackages(readedSeg).ToArray();
            foreach (Serialization.Varint.Package pkg in varintPackages)
            {
                varintPackagesLen += pkg.Head.Count + pkg.Body.Count;
                packages.Add(new Package { Buffer = headBuffer, Segment = pkg.Body });
            }

            var remaining = new ArraySegment<byte>(readedSeg.Array, readedSeg.Offset + varintPackagesLen, readedSeg.Count - varintPackagesLen);

            ArraySegment<byte> remainingHeadVarint = PinionCore.Serialization.Varint.FindVarint(ref remaining);
            if (remainingHeadVarint.Count > 0)
            {
                var offset = PinionCore.Serialization.Varint.BufferToNumber(remaining.Array, remaining.Offset, out int bodySize);
                Memorys.Buffer body = _Pool.Alloc(bodySize);
                _CopyBuffer(remaining, offset, body.Bytes, 0, remaining.Count - offset);
                var remainingBodySize = bodySize - (remaining.Count - offset);
                if (remainingBodySize > 0)
                {
                    var readOffset = bodySize - remainingBodySize;
                    var bodyReadCount = 0;
                    while (bodyReadCount < remainingBodySize)
                    {
                        var readed = await _ReadFromStream(body.Bytes.Array, body.Bytes.Offset + readOffset + bodyReadCount, remainingBodySize - bodyReadCount);

                        if (readed == 0)
                            return new List<PinionCore.Memorys.Buffer>();
                        bodyReadCount += readed;
                    }
                }
                packages.Add(new Package { Buffer = body, Segment = new ArraySegment<byte>(body.Bytes.Array, body.Bytes.Offset, bodySize) });
                return _Convert(packages).ToList();
            }
            if (varintPackages.Length == 0)
                throw new SystemException("head size greater than 8.");
            var buffers = _Convert(packages).ToList();
            if (remaining.Count > 0)
            {
                List<Memorys.Buffer> nestBuffers = await ReadBuffers(new Package { Buffer = headBuffer, Segment = remaining });
                buffers.AddRange(nestBuffers);
            }
            return buffers;
        }

        IEnumerable<PinionCore.Memorys.Buffer> _Convert(IEnumerable<Package> packages)
        {
            foreach (Package pkg in packages)
            {
                Memorys.Buffer buf = _Pool.Alloc(pkg.Segment.Count);
                _CopyBuffer(pkg.Segment, buf.Bytes, pkg.Segment.Count);
                yield return buf;
            }
        }

        private void _CopyBuffer(ArraySegment<byte> source, ArraySegment<byte> destination, int count)
        {
            Array.Copy(source.Array, source.Offset, destination.Array, destination.Offset, count);
        }

        private void _CopyBuffer(ArraySegment<byte> source, int sourceOffset, ArraySegment<byte> destination, int destOffset, int count)
        {
            Array.Copy(source.Array, source.Offset + sourceOffset, destination.Array, destination.Offset + destOffset, count);
        }

        private IWaitableValue<int> _ReadFromStream(byte[] buffer, int offset, int count)
        {
            return _Stream.Receive(buffer, offset, count);

        }
    }
}
