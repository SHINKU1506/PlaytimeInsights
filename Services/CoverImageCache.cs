using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace PlaytimeInsights.Services
{
    internal struct CoverFileStamp
    {
        public long Length { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
    }

    internal interface ICoverFileStampProvider
    {
        bool TryGetStamp(string path, out CoverFileStamp stamp);
    }

    internal interface ICoverImageDecoder
    {
        // Decoders may return unfrozen values; the cache normalizes them
        // with Freeze() before commit.
        BitmapSource Decode(string path, int decodePixelWidth);
    }

    internal sealed class CoverFileStampProvider : ICoverFileStampProvider
    {
        public bool TryGetStamp(string path, out CoverFileStamp stamp)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    stamp = default(CoverFileStamp);
                    return false;
                }

                stamp = new CoverFileStamp
                {
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc
                };
                return true;
            }
            catch
            {
                stamp = default(CoverFileStamp);
                return false;
            }
        }
    }

    public sealed class CoverImageCache
    {
        private readonly object gate = new object();
        private readonly int capacity;
        private readonly ICoverFileStampProvider stampProvider;
        private readonly ICoverImageDecoder decoder;
        private readonly Dictionary<CacheKey, CacheEntry> entries;
        private readonly LinkedList<CacheKey> lru;

        public CoverImageCache(int capacity)
            : this(capacity, new CoverFileStampProvider(), new BitmapImageDecoder())
        {
        }

        internal CoverImageCache(
            int capacity,
            ICoverFileStampProvider stampProvider,
            ICoverImageDecoder decoder)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            this.stampProvider = stampProvider;
            this.decoder = decoder;
            entries = new Dictionary<CacheKey, CacheEntry>();
            lru = new LinkedList<CacheKey>();
        }

        public BitmapSource GetOrLoad(string path, int decodePixelWidth)
        {
            if (string.IsNullOrWhiteSpace(path) || decodePixelWidth <= 0)
            {
                return null;
            }

            string normalizedPath;
            try
            {
                normalizedPath = Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }

            var key = new CacheKey(normalizedPath, decodePixelWidth);

            CoverFileStamp stamp;
            if (!TryGetStamp(normalizedPath, out stamp))
            {
                Remove(key);
                return null;
            }

            BitmapSource cached;
            if (TryGetCurrent(key, stamp, out cached))
            {
                return cached;
            }

            BitmapSource decoded;
            try
            {
                decoded = decoder.Decode(normalizedPath, decodePixelWidth);
            }
            catch
            {
                Remove(key);
                return null;
            }

            if (decoded == null)
            {
                Remove(key);
                return null;
            }

            if (!decoded.IsFrozen)
            {
                try
                {
                    decoded.Freeze();
                }
                catch
                {
                    Remove(key);
                    return null;
                }
            }

            return Commit(key, stamp, decoded);
        }

        private bool TryGetStamp(string path, out CoverFileStamp stamp)
        {
            try
            {
                return stampProvider.TryGetStamp(path, out stamp);
            }
            catch
            {
                stamp = default(CoverFileStamp);
                return false;
            }
        }

        private bool TryGetCurrent(
            CacheKey key,
            CoverFileStamp stamp,
            out BitmapSource image)
        {
            lock (gate)
            {
                CacheEntry entry;
                if (entries.TryGetValue(key, out entry) &&
                    entry.Stamp.Length == stamp.Length &&
                    entry.Stamp.LastWriteTimeUtc == stamp.LastWriteTimeUtc)
                {
                    lru.Remove(entry.Node);
                    lru.AddFirst(entry.Node);
                    image = entry.Image;
                    return true;
                }

                image = null;
                return false;
            }
        }

        private BitmapSource Commit(
            CacheKey key,
            CoverFileStamp stamp,
            BitmapSource decoded)
        {
            lock (gate)
            {
                CacheEntry existing;
                if (entries.TryGetValue(key, out existing) &&
                    existing.Stamp.Length == stamp.Length &&
                    existing.Stamp.LastWriteTimeUtc == stamp.LastWriteTimeUtc)
                {
                    lru.Remove(existing.Node);
                    lru.AddFirst(existing.Node);
                    return existing.Image;
                }

                if (existing != null)
                {
                    lru.Remove(existing.Node);
                    entries.Remove(key);
                }

                var node = lru.AddFirst(key);
                entries[key] = new CacheEntry(stamp, decoded, node);

                while (entries.Count > capacity)
                {
                    var last = lru.Last;
                    if (last == null)
                    {
                        break;
                    }

                    lru.RemoveLast();
                    entries.Remove(last.Value);
                }

                return decoded;
            }
        }

        private void Remove(CacheKey key)
        {
            lock (gate)
            {
                CacheEntry entry;
                if (entries.TryGetValue(key, out entry))
                {
                    lru.Remove(entry.Node);
                    entries.Remove(key);
                }
            }
        }

        private struct CacheKey : IEquatable<CacheKey>
        {
            private readonly string path;
            private readonly int decodePixelWidth;

            public CacheKey(string path, int decodePixelWidth)
            {
                this.path = path;
                this.decodePixelWidth = decodePixelWidth;
            }

            public bool Equals(CacheKey other)
            {
                return decodePixelWidth == other.decodePixelWidth &&
                    string.Equals(
                        path,
                        other.path,
                        StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is CacheKey && Equals((CacheKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.OrdinalIgnoreCase.GetHashCode(path) * 397) ^
                        decodePixelWidth;
                }
            }
        }

        private sealed class CacheEntry
        {
            public CacheEntry(
                CoverFileStamp stamp,
                BitmapSource image,
                LinkedListNode<CacheKey> node)
            {
                Stamp = stamp;
                Image = image;
                Node = node;
            }

            public CoverFileStamp Stamp { get; }
            public BitmapSource Image { get; }
            public LinkedListNode<CacheKey> Node { get; }
        }

        private sealed class BitmapImageDecoder : ICoverImageDecoder
        {
            public BitmapSource Decode(string path, int decodePixelWidth)
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.DecodePixelWidth = decodePixelWidth;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
    }
}
