using System.Collections.Generic;
using System.Drawing;
using System.Collections;
using System;

namespace PhotoScreensaverPlus.Draw
{
    /// <summary>
    /// Class implements bitmap cache to speed up loading viewed images
    /// 
    /// Záznamu, který je z cache vyzvednutý se nastaví vyšší hodnota Age
    /// Pokud počet záznamů překročí nastavenou maximální velikost,
    /// smaže se nejstarší záznam (s nejnižším Age)
    /// </summary>
    public partial class ImageCache:List<ImageCacheEntry>
    {
        public int MaxSize { get; set; }
        private long CurrentAge { get; set; }

        public ImageCache(int maxCacheSize)
        {
            MaxSize = maxCacheSize;
        }

        public new void Add(ImageCacheEntry entry)
        {
            entry.Age = CurrentAge++;
            base.Add(entry);
            if(base.Count > MaxSize)
                removeOldman();
        }

        private void removeOldman()
        {
            ImageCacheEntry oldMan = null;
            Enumerator e = base.GetEnumerator();
            while(e.MoveNext())
            {
                if(null == oldMan)
                    oldMan = e.Current;
                else if(e.Current.Age < oldMan.Age)
                {
                    oldMan = e.Current;
                }
            }
            base.Remove(oldMan);
            oldMan.ExifDictionary.Clear();
            oldMan.InterpolatedBitmap.Dispose();
        }

        /// <summary>
        /// Záznamu, který je z cache vyzvednutý se nastaví vyšší hodnota Age
        /// Pokud počet záznamů překročí nastavenou maximální velikost,
        /// smaže se nejstarší záznam (s nejnižším Age)
        /// </summary>
        /// <param name="FullName"></param>
        /// <returns></returns>
        public ImageCacheEntry Get(string FullName)
        {
            ImageCacheEntry result = base.Find(delegate(ImageCacheEntry bce) { return bce.FullName == FullName; });
            if(null != result)
                result.Age = CurrentAge++;
            return result;
        }
    }

    /// <summary>
    /// Entry for image cache
    /// </summary>
    public partial class ImageCacheEntry
    {
        public string FullName { get; set; }
        public IDictionary<String, String> ExifDictionary { get; set; } //exif dictionary
        public Bitmap InterpolatedBitmap { get; set; } //interpoladed and rotated image
        public long Age { get; set; } //urcuje stari (cim nizsi, tim starsi v historii zobrazovani)
        public ImageCacheEntry(string fullName, Bitmap interpolatedBitmap, IDictionary<String, String> exifHashtable)
        {
            FullName = fullName;
            ExifDictionary = exifHashtable;
            InterpolatedBitmap = interpolatedBitmap;
        }
    }
}
