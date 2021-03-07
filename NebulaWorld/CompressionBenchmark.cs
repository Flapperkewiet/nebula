using NebulaModel.Logger;
using System.IO;
using System.IO.Compression;
using LZ4;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Lz4Net;
using XZ.NET;

namespace NebulaWorld
{
    public static class CompressionBenchmark
    {
        public delegate long Benchmark();
        public const int iterations = 10;

        public static void RunAllBenchmarks()
        {
            RunBenchmark(LZ4AltFastBuffered4K);
            RunBenchmark(LZ4AltFastBuffered8K);
            return;
            RunBenchmark(RawBinary);
            RunBenchmark(BuiltInGZip);
            RunBenchmark(BuiltInGZipBuffered4K);
            RunBenchmark(BuiltInGZipBuffered8K);
            RunBenchmark(LZ4);
            RunBenchmark(LZ4HighCompression);
            RunBenchmark(LZ4AltFast);
            RunBenchmark(LZ4AltHighCompression);
            RunBenchmark(XZ);
        }

        public static void RunBenchmark(Benchmark function)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                List<long> bytesInStream = new List<long>();
                List<double> timesPerIteration = new List<double>();
                for (int i = 0; i < iterations; i++)
                {
                    sw.Reset();
                    sw.Start();
                    bytesInStream.Add(function());
                    sw.Stop();
                    timesPerIteration.Add(sw.Elapsed.TotalMilliseconds);
                }
                Log.Info(function.Method.Name);
                Log.Info($"\t (min, max, avg) time in ms ({(int)timesPerIteration.Min()}, {(int)timesPerIteration.Max()}, {(int)timesPerIteration.Average()})");
                double kB = (int)bytesInStream.Average() / 1000.0d;
                double mB = kB / 1000.0d;
                Log.Info($"\t Size: {(int)bytesInStream.Average()} bytes = {kB:N2}KB = {mB:N2}MB");
            }
            catch (System.Exception e)
            {
                Log.Error(function.Method.Name);
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }

        public static long RawBinary()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    GameMain.data.Export(bw);
                }
                return ms.ToArray().Length;
            }
        }

        public static long BuiltInGZip()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (BinaryWriter bw = new BinaryWriter(gzip))
                    {
                        GameMain.data.Export(bw);
                    }
                }
                return ms.ToArray().Length;
            }
        }

        public static long BuiltInGZipBuffered4K()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    using (BufferedStream bs = new BufferedStream(gzip, 4096))
                    {
                        using (BinaryWriter bw = new BinaryWriter(bs))
                        {
                            GameMain.data.Export(bw);
                        }
                    }
                }
                return ms.ToArray().Length;
            }
        }

        public static long BuiltInGZipBuffered8K()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    using (BufferedStream bs = new BufferedStream(gzip, 8192))
                    {
                        using (BinaryWriter bw = new BinaryWriter(bs))
                        {
                            GameMain.data.Export(bw);
                        }
                    }
                }
                return ms.ToArray().Length;
            }
        }

        public static long LZ4()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (LZ4Stream lz4 = new LZ4Stream(ms, CompressionMode.Compress, LZ4StreamFlags.Default))
                {
                    using (BinaryWriter bw = new BinaryWriter(lz4))
                    {
                        GameMain.data.Export(bw);
                    }
                }
                return ms.ToArray().Length;
            }
        }

        public static long LZ4HighCompression()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (LZ4Stream lz4 = new LZ4Stream(ms, CompressionMode.Compress, LZ4StreamFlags.HighCompression))
                {
                    using (BinaryWriter bw = new BinaryWriter(lz4))
                    {
                        GameMain.data.Export(bw);
                    }
                }
                return ms.ToArray().Length;
            }
        }
        public static long LZ4AltFast()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Lz4CompressionStream lz4 = new Lz4CompressionStream(ms, Lz4Mode.Fast))
                {
                    using (BinaryWriter bw = new BinaryWriter(lz4))
                    {
                        GameMain.data.Export(bw);
                    }
                }
                return ms.ToArray().Length;
            }
        }

        public static long LZ4AltFastBuffered4K()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Lz4CompressionStream lz4 = new Lz4CompressionStream(ms, Lz4Mode.Fast))
                {
                    using (BufferedStream bs = new BufferedStream(lz4, 4096))
                    {
                        using (BinaryWriter bw = new BinaryWriter(bs))
                        {
                            GameMain.data.Export(bw);
                        }
                    }
                }
                return ms.ToArray().Length;
            }
        }

        public static long LZ4AltFastBuffered8K()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Lz4CompressionStream lz4 = new Lz4CompressionStream(ms, Lz4Mode.Fast))
                {
                    using (BufferedStream bs = new BufferedStream(lz4, 8192))
                    {
                        using (BinaryWriter bw = new BinaryWriter(bs))
                        {
                            GameMain.data.Export(bw);
                        }
                    }
                }
                return ms.ToArray().Length;
            }
        }

        public static long LZ4AltHighCompression()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Lz4CompressionStream lz4 = new Lz4CompressionStream(ms, Lz4Mode.HighCompression))
                {
                    using (BinaryWriter bw = new BinaryWriter(lz4))
                    {
                        GameMain.data.Export(bw);
                    }
                }
                return ms.ToArray().Length;
            }
        }
        public static long XZ()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (XZInputStream xz = new XZInputStream(ms))
                {
                    using (BinaryWriter bw = new BinaryWriter(xz))
                    {
                        GameMain.data.Export(bw);
                    }
                }
                return ms.ToArray().Length;
            }
        }
    }
}
