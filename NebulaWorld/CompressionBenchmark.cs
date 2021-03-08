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
        public delegate long Benchmark(out byte[] data);
        public delegate void DecompressionBenchmark(byte[] data);

        public const int iterations = 15;

        public static void RunAllBenchmarks()
        {
            
            RunBenchmark(RawBinary);
            RunBenchmark(BuiltInGZip);
            RunBenchmark(BuiltInGZipBuffered4K);
            RunBenchmark(BuiltInGZipBuffered8K);
            RunBenchmark(LZ4);
            RunBenchmark(LZ4AltFast);
            RunBenchmark(LZ4AltFastBuffered4K);
            RunBenchmark(LZ4AltFastBuffered8K);

            
            byte[] data;
            RawBinary(out data);
            RunDecompressionBenchmark(RawBinaryDecompression, data);

            BuiltInGZip(out data);
            RunDecompressionBenchmark(BuiltInGZipDecompression, data);

            BuiltInGZipBuffered4K(out data);
            RunDecompressionBenchmark(BuiltInGZipBuffered4KDecompression, data);

            BuiltInGZipBuffered8K(out data);
            RunDecompressionBenchmark(BuiltInGZipBuffered8KDecompression, data);

            LZ4(out data);
            RunDecompressionBenchmark(LZ4Decompression, data);

            LZ4AltFast(out data);
            RunDecompressionBenchmark(LZ4AltFastDecompression, data);

            LZ4AltFastBuffered4K(out data);
            RunDecompressionBenchmark(LZ4AltFastBuffered4KDecompression, data);

            LZ4AltFastBuffered8K(out data);
            RunDecompressionBenchmark(LZ4AltFastBuffered8KDecompression, data);
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
                    byte[] data;
                    bytesInStream.Add(function(out data));
                    sw.Stop();
                    if (i >= 5)
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

        public static void RunDecompressionBenchmark(DecompressionBenchmark function, byte[] data)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                List<double> timesPerIteration = new List<double>();
                Log.Info("Compressed size: " + data.Length);
                for (int i = 0; i < iterations; i++)
                {
                    sw.Reset();
                    sw.Start();
                    function(data);
                    sw.Stop();
                    if (i >= 5)
                        timesPerIteration.Add(sw.Elapsed.TotalMilliseconds);
                }
                Log.Info(function.Method.Name);
                Log.Info($"\t (min, max, avg) time in ms ({(int)timesPerIteration.Min()}, {(int)timesPerIteration.Max()}, {(int)timesPerIteration.Average()})");
            }
            catch (System.Exception e)
            {
                Log.Error(function.Method.Name);
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
            }
        }

        public static void RawBinaryDecompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    GameMain.data.Import(br);
                }
            }
        }

        public static long RawBinary(out byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    GameMain.data.Export(bw);
                }
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }

        public static void BuiltInGZipDecompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (BinaryReader br = new BinaryReader(gzip))
                    {
                        GameMain.data.Import(br);
                    }
                }
            }
        }

        public static long BuiltInGZip(out byte[] data)
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
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }

        public static void BuiltInGZipBuffered4KDecompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    using (BufferedStream bs = new BufferedStream(gzip, 4096))
                    {
                        using (BinaryReader br = new BinaryReader(bs))
                        {
                            GameMain.data.Import(br);
                        }
                    }
                }
            }
        }
        public static long BuiltInGZipBuffered4K(out byte[] data)
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
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }

        public static void BuiltInGZipBuffered8KDecompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    using (BufferedStream bs = new BufferedStream(gzip, 8192))
                    {
                        using (BinaryReader br = new BinaryReader(bs))
                        {
                            GameMain.data.Import(br);
                        }
                    }
                }
            }
        }

        public static long BuiltInGZipBuffered8K(out byte[] data)
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
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }

        public static void LZ4Decompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (LZ4Stream lz4 = new LZ4Stream(ms, CompressionMode.Decompress, LZ4StreamFlags.Default))
                {
                    using (BinaryReader br = new BinaryReader(lz4))
                    {
                        GameMain.data.Import(br);
                    }
                }
            }
        }

        public static long LZ4(out byte[] data)
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
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }

        public static void LZ4AltFastDecompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (Lz4DecompressionStream lz4 = new Lz4DecompressionStream(ms))
                {
                    using (BinaryReader br = new BinaryReader(lz4))
                    {
                        GameMain.data.Import(br);
                    }
                }
            }
        }

        public static long LZ4AltFast(out byte[] data)
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
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }


        public static void LZ4AltFastBuffered4KDecompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (Lz4DecompressionStream lz4 = new Lz4DecompressionStream(ms))
                {
                    using (BufferedStream bs = new BufferedStream(lz4, 4096))
                    {
                        using (BinaryReader br = new BinaryReader(bs))
                        {
                            GameMain.data.Import(br);
                        }
                    }
                }
            }
        }

        public static long LZ4AltFastBuffered4K(out byte[] data)
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
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }

        public static void LZ4AltFastBuffered8KDecompression(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (Lz4DecompressionStream lz4 = new Lz4DecompressionStream(ms))
                {
                    using (BufferedStream bs = new BufferedStream(lz4, 8192))
                    {
                        using (BinaryReader br = new BinaryReader(bs))
                        {
                            GameMain.data.Import(br);
                        }
                    }
                }
            }
        }

        public static long LZ4AltFastBuffered8K(out byte[] data)
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
                data = ms.ToArray();
                return ms.ToArray().Length;
            }
        }

    }
}
