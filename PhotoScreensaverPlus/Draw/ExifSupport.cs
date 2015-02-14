using System;

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using PhotoScreensaverPlus.Logging;
using NLog;

namespace PhotoScreensaverPlus.Draw
{
    /// <summary>
    /// Static methods to decode EXIF values from the image
    /// Based on article at http://blogs.msdn.com/coding4fun/archive/2007/05/11/2553866.aspx
    /// </summary>
    public class ExifSupport
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        //Endian endian; //byte order

        //used in FormatTagRational
        private const int BYTEJUMP_LONG = 4;
        //used in FormatTagRational
        private const int BYTEJUMP_RATIONAL = 8;
        //used in FormatTagRational
        public const string DOUBLETYPE_FORMAT = "0.0####";
        public const string DOUBLETYPE_FORMAT2 = "0.#####";

        public const string IMAGE_TITLE = "title";
        public const string IMAGE_DESCRIPTION = "description";
        public const string IMAGE_COMMENT = "commnent";
        public const string IMAGE_DATE = "date";
        public const string IMAGE_TIME = "time";
        public const string IMAGE_CAMERA_MODEL = "cameraModel";
        public const string IMAGE_EXPOSURE_TIME = "exposureTime";
        public const string IMAGE_LENS_APERTURE = "lensAperture";
        public const string IMAGE_FOCAL_LENGHT = "focalLenght";
        public const string IMAGE_FLASH_FIRED = "flashFired";
        public const string IMAGE_ISO_SPEED = "isoSpeed";
        public const string IMAGE_ROTATION = "rotation";

        /// <summary>
        /// Return dictionary with exif informations
        /// </summary>
        /// <param name="bitmap">Original bitmap with exif</param>
        public static IDictionary<String, String> GetExifDictionary(Bitmap originalBitmap)
        {

            IDictionary<String, String> exifHashtable = new Dictionary<String, String>();
            try
            {
                //aktuální codepage uživatele
                int codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
                Encoding encoding = Encoding.GetEncoding(codePage);

                exifHashtable.Add(IMAGE_TITLE, GetExifString(originalBitmap, 0x0320, encoding));
                exifHashtable.Add(IMAGE_DESCRIPTION, GetExifString(originalBitmap, 0x010E, encoding));

                //UserComment typicky obsahuje klíčové slovo UNICODE, po kterém teprve následuje zakódovaný text
                exifHashtable.Add(IMAGE_COMMENT, GetImageUserComment(originalBitmap));

                string ImageExifDTOrig = GetExifString(originalBitmap, 0x9003, encoding);
                string ImageExifDateOrig = "", ImageExifTimeOrig = "";
                //try to parse date and time
                if (null != ImageExifDTOrig)
                    try
                    {
                        DateTime dt = DateTime.ParseExact(ImageExifDTOrig, "yyyy:MM:dd HH:mm:ss", null);
                        ImageExifDateOrig = dt.ToShortDateString();
                        ImageExifTimeOrig = dt.ToShortTimeString();
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal("Can't draw exif date and time of the file", ex);
                        //WindowsLogWriter.WriteLog("DrawExif - Can't draw exif date and time of the file, exception = " + ex.Message, EventLogEntryType.Error);
                    }
                exifHashtable.Add(IMAGE_DATE, ImageExifDateOrig);
                exifHashtable.Add(IMAGE_TIME, ImageExifTimeOrig);

                exifHashtable.Add(IMAGE_CAMERA_MODEL, GetExifString(originalBitmap, 0x0110, encoding));
                exifHashtable.Add(IMAGE_EXPOSURE_TIME, GetExifRationalStringFormat(originalBitmap, 0x829A, "", FormatInstr.FRACTION, null));
                exifHashtable.Add(IMAGE_LENS_APERTURE, GetExifRationalStringFormat(originalBitmap, 0x9202, "", FormatInstr.NO_OP, ExifSupport.DOUBLETYPE_FORMAT));
                exifHashtable.Add(IMAGE_FOCAL_LENGHT, GetExifRationalStringFormat(originalBitmap, 0x920A, "", FormatInstr.NO_OP, ExifSupport.DOUBLETYPE_FORMAT2));

                string FlashFired;
                short flash = ExifSupport.GetExifShort(originalBitmap, 0x9209, 0x0005);
                switch (flash)
                {
                    case 0:
                        FlashFired = "Flash did not fire";
                        break;
                    case 1:
                        FlashFired = "Flash fired";
                        break;
                    /* Je to zakomentované, protože se to ukazuje, když není žádný EXIF
                    case 5:
                        FlashFired = "Strobe return light not detected";
                        break;*/
                    case 7:
                        FlashFired = "Strobe return light detected";
                        break;
                    case 9:
                        FlashFired = "Flash fired, compulsory flash mode";
                        break;
                    case 13:
                        FlashFired = "Flash fired, compulsory flash mode, return light not detected";
                        break;
                    case 15:
                        FlashFired = "Flash fired, compulsory flash mode, return light detected";
                        break;
                    case 16:
                        FlashFired = "Flash did not fire, compulsory flash mode";
                        break;
                    case 24:
                        FlashFired = "Flash did not fire, auto mode";
                        break;
                    case 25:
                        FlashFired = "Flash fired, auto mode";
                        break;
                    case 29:
                        FlashFired = "Flash fired, auto mode, return light not detected";
                        break;
                    case 31:
                        FlashFired = "Flash fired, auto mode, return light detected";
                        break;
                    case 32:
                        FlashFired = "No flash function";
                        break;
                    case 65:
                        FlashFired = "Flash fired, red-eye reduction mode";
                        break;
                    case 69:
                        FlashFired = "Flash fired, red-eye reduction mode, return light not detected";
                        break;
                    case 71:
                        FlashFired = "Flash fired, red-eye reduction mode, return light detected";
                        break;
                    case 73:
                        FlashFired = "Flash fired, compulsory flash mode, red-eye reduction mode";
                        break;
                    case 77:
                        FlashFired = "Flash fired, compulsory flash mode, red-eye reduction mode, return light not detected";
                        break;
                    case 79:
                        FlashFired = "Flash fired, compulsory flash mode, red-eye reduction mode, return light detected";
                        break;
                    case 89:
                        FlashFired = "Flash fired, auto mode, red-eye reduction mode";
                        break;
                    case 93:
                        FlashFired = "Flash fired, auto mode, return light not detected, red-eye reduction mode";
                        break;
                    case 95:
                        FlashFired = "Flash fired, auto mode, return light detected, red-eye reduction mode";
                        break;
                    default:
                        FlashFired = "";
                        break;
                }
                exifHashtable.Add(IMAGE_FLASH_FIRED, FlashFired);

                string ISOSpeed;
                short iso = ExifSupport.GetExifShort(originalBitmap, 0x8827, 0);
                if (iso == 0)
                    ISOSpeed = "";
                else
                    ISOSpeed = Convert.ToString(iso);
                exifHashtable.Add(IMAGE_ISO_SPEED, ISOSpeed);

                string rotation = Convert.ToString(ExifSupport.GetExifShort(originalBitmap, 0x0112, 1));
                exifHashtable.Add(IMAGE_ROTATION, rotation);
            }
            catch (Exception e)
            {
                logger.Fatal("Can't get exif of the file", e);
                //WindowsLogWriter.WriteLog("DrawExif - Can't get exif of the file, exception = " + e.Message, EventLogEntryType.Error);
            }

            return exifHashtable;
        }

        /// <summary>
        /// Vytáhne uživatelský komentář - je to speciální tag, který typicky jako první řetězec obsahuje kódování, ve kterém je
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static string GetImageUserComment(Bitmap bmp)
        {
            String ImageUserComment = null;
            try
            {
                PropertyItem ucPropItem = GetImagePropertyItem(bmp, 0x9286);
                if (ucPropItem != null)
                {
                    UserComment uc = new UserComment(ucPropItem.Value, true);
                    ImageUserComment = uc.Value;
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Can't decode user comment", ex);
                //WindowsLogWriter.WriteLog("DrawExif - Can't decode user comment, exception = " + ex.Message, EventLogEntryType.Error);
            }
            if (null != ImageUserComment && ImageUserComment.Length > 0 && ImageUserComment.Substring(0, 1).Contains("\0"))
                ImageUserComment = ""; //remove empty comment  
            return ImageUserComment;
        }

        /// <summary>
        /// Reads string values with defined encoding (type 2 is a null-terminated string)
        /// </summary>
        /// <param name="bmp">Bitmap image possibly containing tag</param>
        /// <param name="id">Tag ID to decode</param>
        /// <param name="encoding">Encoding of the string</param>
        /// <returns>String version of value, or NULL if property does not exist or is not a string</returns>
        public static string GetExifString(Bitmap bmp, int id, Encoding encoding)
        {
            PropertyItem prop = GetImagePropertyItem(bmp, id);
            if (prop != null)
                return GetExifString(prop, encoding);
            return null;
        }

        /// <summary>
        /// Reads string values with defined encoding (type 2 is a null-terminated string)
        /// </summary>
        /// <param name="prop">PropertyItem for this tag</param>
        /// <param name="encoding">Encoding of the string</param>
        /// <returns>String version of value, or NULL if property does not exist or is not a string</returns>
        public static string GetExifString(PropertyItem prop, Encoding encoding)
        {
            if (prop.Type != 0x2) throw new ArgumentException("Not an EXIF string value");

            //System.Text.Encoding enc = System.Text.UnicodeEncoding.Unicode;
            //System.Text.Encoding enc = System.Text.UTF8Encoding.Unicode;
            if (encoding == null) encoding = System.Text.ASCIIEncoding.ASCII;

            if (prop.Value == null) return null;
            else
            {
                return encoding.GetString(prop.Value, 0, prop.Value.Length - 1).Trim(); // Minus one to remove NULL-terminator
            }
        }

        /// <summary>
        /// Reads SHORT (16-bit) values (type 3 is a 16-bit unsigned integer)
        /// </summary>
        /// <param name="bmp">Bitmap image possibly containing tag</param>
        /// <param name="id">Tag ID to decode</param>
        /// <returns>Short version of value -- an exception is thrown if something is not right</returns>
        public static short GetExifShort(Bitmap bmp, int id, short defaultValue)
        {
            PropertyItem prop = GetImagePropertyItem(bmp, id);
            if (prop != null)
                return GetExifShort(prop);
            return defaultValue;
        }

        /// <summary>
        /// Reads ASCII string values (type 2 is a null-terminated string)
        /// </summary>
        /// <param name="prop">Bitmap image possibly containing tag</param>
        /// <returns>Short version of value -- an exception is thrown if something is not right</returns>
        public static short GetExifShort(PropertyItem prop)
        {
            if (prop.Type != 0x3) throw new ArgumentException("Not an EXIF short value");

            return (short)(prop.Value[0] | prop.Value[1] << 8);
        }

        /// <summary>
        /// Reads long (32-bit) values (type 4 is a 4-byte long)
        /// </summary>
        /// <param name="bmp">Bitmap image possibly containing tag</param>
        /// <param name="id">Tag ID to decode</param>
        /// <param name="defaultValue">Value to return if something is wrong</param>
        /// <returns>Long version of value, or defaultValue</returns>
        public static long GetExifLong(Bitmap bmp, int id, long defaultValue)
        {
            PropertyItem prop = GetImagePropertyItem(bmp, id);
            if (prop != null)
                return GetExifLong(prop);
            return defaultValue;
        }

        /// <summary>
        /// Reads long (32-bit) values (type 4 is a 4-byte long)
        /// </summary>
        /// <param name="prop">PropertyItem for this tag</param>
        /// <returns>Long version of value, or an exception if value is not a long</returns>
        public static long GetExifLong(PropertyItem prop)
        {
            if (prop.Type != 0x4) throw new ArgumentException("Not an EXIF long value");

            return (long)(prop.Value[0] | prop.Value[1] << 8 | prop.Value[2] << 16 | prop.Value[3] << 24);
        }

        /// <summary>
        /// Reads two LONGs (4-byte longs). The first LONG is the numerator and the second LONG expresses the denominator.
        /// </summary>
        /// <param name="prop">PropertyItem for this tag</param>
        /// <returns>Two longs, or an exception if value is not a rational</returns>
        public static long[] GetExifRational(PropertyItem prop)
        {
            if (prop.Type != 0x5) throw new ArgumentException("Not an EXIF rational value");

            long[] result = new long[2];
            result[0] = (long)(prop.Value[0] | prop.Value[1] << 8 | prop.Value[2] << 16 | prop.Value[3] << 24);
            result[1] = (long)(prop.Value[4] << 32 | prop.Value[5] << 40 | prop.Value[6] << 48 | prop.Value[7] << 56);
            return result;
        }

        /// <summary>
        /// Reads two LONGs (4-byte longs). The first LONG is the numerator and the second LONG expresses the denominator.
        /// </summary>
        /// <param name="bmp">Bitmap image possibly containing tag</param>
        /// <param name="id">Tag ID to decode</param>
        /// <param name="defaultValue">Value to return if something is wrong</param>
        /// <returns>Two longs, or defaultValue</returns>
        public static long[] GetExifRational(Bitmap bmp, int id, long[] defaultValue)
        {
            PropertyItem prop = GetImagePropertyItem(bmp, id);
            if (prop != null)
                return GetExifRational(prop);
            return defaultValue;
        }

        /// <summary>
        /// Reads rational type tag and returns string representation
        /// </summary>
        /// <param name="bmp">Bitmap image possibly containing tag</param>
        /// <param name="id">Tag ID to decode</param>
        /// <param name="defaultValue">Value to return if something is wrong</param>
        /// <param name="formatAsFraction">True if result have to be fraction</param>
        /// <param name="doubleFormat">Format for double value</param>
        /// <returns>String representation of property item value</returns>
        public static string GetExifRationalStringFormat(Bitmap bmp, int id, string defaultValue, FormatInstr format, string doubleFormat)
        {
            PropertyItem prop = GetImagePropertyItem(bmp, id);
            if (prop != null)
                return GetExifRationalStringFormat(prop, format, doubleFormat);
            return defaultValue;
        }

        /// <summary>
        /// Reads rational type tag and returns string representation
        /// </summary>
        /// <param name="propItem">PropertyItem for this tag</param>
        /// <param name="formatAsFraction">True if result have to be fraction</param>
        /// <param name="doubleFormat">Format for double value</param>
        /// <returns>String representation of property item value</returns>
        public static string GetExifRationalStringFormat(PropertyItem propItem,
            FormatInstr format, string doubleFormat)
        {
            if (propItem.Type != 0x5) throw new ArgumentException("Not an EXIF rational value");

            string strRet = "";
            for (int i = 0; i < propItem.Len; i = i + BYTEJUMP_RATIONAL)
            {
                System.UInt32 numer = BitConverter.ToUInt32(propItem.Value, i);
                System.UInt32 denom = BitConverter.ToUInt32(propItem.Value, i
                    + BYTEJUMP_LONG);
                if (format == FormatInstr.FRACTION)
                {
                    UFraction frac = new UFraction(numer, denom);
                    strRet += frac.ToString();
                }
                else
                {
                    double dbl;
                    if (denom == 0)
                        dbl = 0.0;
                    else
                        dbl = (double)numer / (double)denom;

                    dbl = Math.Round(dbl, 1, MidpointRounding.AwayFromZero);
                    strRet += dbl.ToString(doubleFormat);
                }
                if (i + BYTEJUMP_RATIONAL < propItem.Len)
                    strRet += " ";
            }
            return strRet;
        }

        /// <summary>
        /// Reads two LONGs (4-byte longs). The first LONG is the numerator and the second LONG expresses the denominator.
        /// </summary>
        /// <param name="bmp">Bitmap image possibly containing tag</param>
        /// <param name="id">Tag ID to decode</param>
        /// <param name="defaultValue">Value to return if something is wrong</param>
        /// <returns>Two longs, or defaultValue</returns>
        public static string GetExifUndefined(Bitmap bmp, int id, FormatInstr format)
        {
            PropertyItem prop = GetImagePropertyItem(bmp, id);
            if (prop != null)
                return GetExifUndefined(prop, format);
            return null;
        }

        /// <summary>Format a Undefined tag.</summary>
        public static string GetExifUndefined(PropertyItem propItem, FormatInstr formatInstr)
        {
            string strRet = null;
            if (propItem != null && propItem.Value.Length > 0)
            {
                if (formatInstr == FormatInstr.ALLCHAR)
                {
                    //if the text begins with "UNICODE"
                    if ((propItem.Value.Length >= ("UNICODE").Length) && System.Text.Encoding.ASCII.GetString(propItem.Value, 0, ("UNICODE").Length).Equals("UNICODE"))
                    {
                        System.Text.UnicodeEncoding encoding = new System.Text.UnicodeEncoding();
                        strRet = encoding.GetString(propItem.Value, 0, propItem.Len);
                    }
                    else
                    {
                        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                        strRet = encoding.GetString(propItem.Value, 0, propItem.Len);
                    }
                }
                else
                    strRet = BitConverter.ToString(propItem.Value, 0, propItem.Len);
            }
            return strRet;
        }

        /// <summary>
        /// Najde mezi vlastnostmi obrázku tu správnou podle ID, případně vrátí NULL
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static PropertyItem GetImagePropertyItem(Bitmap bmp, int id)
        {
            PropertyItem[] propItems = bmp.PropertyItems;
            for (int i = 0; i < propItems.Length; i++)
            {
                if (propItems[i].Id == id)
                {
                    return propItems[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Rotates image in 90 degree
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="rotateRight">true to rotate right, false to rotate left</param>
        public static void RotateImage(FileInfo fileInfo, bool rotateRight)
        {
            System.Drawing.Imaging.Encoder Enc = System.Drawing.Imaging.Encoder.Transformation;
            EncoderParameters EncParms = new EncoderParameters(1);
            EncoderParameter EncParm;
            ImageCodecInfo CodecInfo = GetEncoderInfo("image/jpeg");
            FileStream fileStream = null;
            try
            {
                fileStream = fileInfo.OpenRead();
                Image img = Image.FromStream(fileStream);

                if (rotateRight)
                    EncParm = new EncoderParameter(Enc, (long)EncoderValue.TransformRotate90);
                else
                    EncParm = new EncoderParameter(Enc, (long)EncoderValue.TransformRotate270);
                EncParms.Param[0] = EncParm;

                img.Save(fileInfo.FullName + ".rotated", CodecInfo, EncParms);

                // for computers with low memory and large pictures: release memory now
                img.Dispose();
                img = null;
                GC.Collect();
                fileStream.Close();
                fileStream.Dispose();

                System.IO.File.Replace(fileInfo.FullName + ".rotated", fileInfo.FullName, fileInfo.FullName + ".backup", true);

                System.IO.File.Delete(fileInfo.FullName + ".backup");
            }
            catch (Exception e)
            {
                logger.Fatal("Can't rotate image", e);
                //WindowsLogWriter.WriteLog("RotateImage - Can't rotate image: " + e.Message, EventLogEntryType.Error);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        //writes new metadata to the image
        //údajně je možné při použití rotace zapsat do exifu bezestrátově
        //see http://www.eggheadcafe.com/articles/20030706.asp
        public static void WriteNewMetadata(FileInfo fileInfo, string NewTitle, string NewDescription, string NewUserComment)
        {
            string FilenameTemp = "";
            System.Drawing.Imaging.Encoder Enc = System.Drawing.Imaging.Encoder.Transformation;
            EncoderParameters EncParms = new EncoderParameters(1);
            EncoderParameter EncParm;
            ImageCodecInfo CodecInfo = GetEncoderInfo("image/jpeg");

            PropertyItem[] PropertyItems;

            //aktuální codepage uživatele
            int codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
            Encoding currentEncoding = Encoding.GetEncoding(codePage);

            byte[] bTitle = currentEncoding.GetBytes(NewTitle);
            byte[] bDescription = currentEncoding.GetBytes(NewDescription);

            //ImageUserComment - ten je nejkomplikovanější - musíme to překódovat do UNICODE
            //prvních 8 znaků popisuje kódování, ve kterém to bude
            byte[] unicodeBytes = Encoding.ASCII.GetBytes("UNICODE\0");
            // Convert the string into a byte[]. 
            byte[] currentEncodingBytes = currentEncoding.GetBytes(NewUserComment); //potřebujeme tam přidat klíčové slovo UNICODE a pak to celé zakódovat
            //zakódujeme to do UNICODE
            Encoding targetEncoding = new System.Text.UnicodeEncoding(false, true);
            // Perform the conversion from one encoding to the other. 
            byte[] userCommentByteArray = Encoding.Convert(currentEncoding, targetEncoding,
            currentEncodingBytes);

            //teď musíme spojit ty dvě pole bytů
            byte[] bUserComment = new byte[unicodeBytes.Length + userCommentByteArray.Length];
            Array.Copy(unicodeBytes, 0, bUserComment, 0, unicodeBytes.Length);
            Array.Copy(userCommentByteArray, 0, bUserComment, unicodeBytes.Length, userCommentByteArray.Length);

            FileStream fileStream = null;
            try
            {
                fileStream = fileInfo.OpenRead();
                Image img = Image.FromStream(fileStream);

                // put the new description into the right property item
                PropertyItems = img.PropertyItems;

                PropertyItems[0].Id = 0x0320; //image title
                PropertyItems[0].Type = 2;
                PropertyItems[0].Len = bTitle.Length;
                PropertyItems[0].Value = bTitle;
                img.SetPropertyItem(PropertyItems[0]);

                PropertyItems[0].Id = 0x010e; //image description
                PropertyItems[0].Type = 2;
                PropertyItems[0].Len = bDescription.Length;
                PropertyItems[0].Value = bDescription;
                img.SetPropertyItem(PropertyItems[0]);

                PropertyItems[0].Id = 0x9286; //user comment
                PropertyItems[0].Type = 7;
                PropertyItems[0].Len = bUserComment.Length; //user comment obsahuje prvníh 8 znaků kódování (UNICODE\0)
                PropertyItems[0].Value = bUserComment;
                img.SetPropertyItem(PropertyItems[0]);

                // we cannot store in the same image, so use a temporary image instead
                FilenameTemp = fileInfo.FullName + ".temp";

                // for lossless rewriting must rotate the image by 90 degrees!
                EncParm = new EncoderParameter(Enc, (long)EncoderValue.TransformRotate90);
                EncParms.Param[0] = EncParm;

                // now write the rotated image with new description
                if (File.Exists(FilenameTemp))
                    System.IO.File.Delete(FilenameTemp);
                img.Save(FilenameTemp, CodecInfo, EncParms);

                // for computers with low memory and large pictures: release memory now
                img.Dispose();
                img = null;
                GC.Collect();

                fileStream.Close();
                fileStream.Dispose();

                // delete the original file, will be replaced later
                fileInfo.Delete();

                // now must rotate back the written picture
                img = Image.FromFile(FilenameTemp);
                EncParm = new EncoderParameter(Enc, (long)EncoderValue.TransformRotate270);
                EncParms.Param[0] = EncParm;
                img.Save(fileInfo.FullName, CodecInfo, EncParms);

                // release memory now
                img.Dispose();
                img = null;
                GC.Collect();

                // delete the temporary picture
                System.IO.File.Delete(FilenameTemp);

            }
            catch (Exception e)
            {
                logger.Fatal("Can't save new exif information", e);
                //WindowsLogWriter.WriteLog("WriteNewMetadata - Can't save new exif information: " + e.Message, EventLogEntryType.Error);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }

        //returns encoder for specific mimeType
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            } return null;
        }
    }
    /// <summary>
    /// Helper class, represents fractions of Exif rational type
    /// </summary>
    public class UFraction
    {
        private UInt32 _numer;
        private UInt32 _denom;

        public UFraction(UInt32 numer, UInt32 denom)
        {
            _numer = numer;
            _denom = denom;
        }

        public override string ToString()
        {
            UInt32 numer = _numer;
            UInt32 denom = (_denom == 0) ? 1 : _denom;

            Reduce(ref numer, ref denom);

            string result;
            if (numer == 0)
                result = "0";
            else if (denom == 1)
                result = numer + "";
            else
                result = numer + "/" + denom;

            return result;
        }

        private static void Reduce(ref UInt32 numer, ref UInt32 denom)
        {
            if (numer != 0)
            {
                UInt32 common = GCD(numer, denom);

                numer = numer / common;
                denom = denom / common;

                //if there is problem with accuracy of recorded exif value
                if (numer > 1)
                {
                    denom = (UInt32)Math.Truncate((double)denom / (double)numer);
                    numer = 1;
                }
            }
        }

        private static UInt32 GCD(UInt32 num1, UInt32 num2)
        {
            while (num1 != num2)
                if (num1 > num2)
                    num1 = num1 - num2;
                else
                    num2 = num2 - num1;

            return num1;
        }
    }

    public enum FormatInstr
    {
        /// <summary>No formatting instruction</summary>
        NO_OP,
        /// <summary>Instruction to change the value to a fraction.</summary>
        /// <remarks>This is only applicable to RATIONAL and SRATIONAL tags.</remarks>
        FRACTION,
        /// <summary>Instruction to format the bytes as a non-null terminated string.</summary>
        /// <remarks>This is only applicable to UNDEFINED tags.</remarks>
        ALLCHAR,
        /// <summary>Instruction to format the bytes as a Base-64 string.</summary>
        /// <remarks>This is only applicable to BYTE tags.</remarks>
        BASE64
    }

    //Structure taken from "F-SPOT source code" - http://www.koders.com/csharp/fidF6632006F25B8E5B3BCC62D13076B38D71847929.aspx?s=zoom#L233
    struct UserComment
    {
        string Charset;
        public string Value;

        public UserComment(string value)
        {
            Charset = null;
            Value = value;
        }

        public UserComment(byte[] raw_data, bool little)
        {
            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            Charset = "ASCII\0\0\0";
            Value = enc.GetString(raw_data, 0, raw_data.Length);

            if (raw_data.Length >= 8)
            {
                string charset = System.Text.Encoding.ASCII.GetString(raw_data, 0, 8);
                switch (charset)
                {
                    case "ASCII\0\0\0":
                        enc = System.Text.Encoding.ASCII;
                        Charset = charset;
                        Value = enc.GetString(raw_data, 8, raw_data.Length - 8);
                        break;
                    case "UNICODE\0":
                    case "Unicode\0":
                        enc = new System.Text.UnicodeEncoding(!little, true);
                        Charset = charset;
                        Value = enc.GetString(raw_data, 8, raw_data.Length - 8);
                        break;
                    case "JIS\0\0\0\0\0":
                        // FIXME this requires mono locale extras
                        try
                        {
                            enc = System.Text.Encoding.GetEncoding("euc-jp");
                            Charset = charset;
                            Value = enc.GetString(raw_data, 8, raw_data.Length - 8);
                        }
                        catch
                        {
                            enc = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
                            Charset = charset;
                            Value = enc.GetString(raw_data, 8, raw_data.Length - 8);
                        }
                        break;
                    case "\0\0\0\0\0\0\0\0":
                        // FIXME the spec says to use the local encoding in this case, we could probably
                        // do something smarter, but whatever.
                        Value = enc.GetString(raw_data, 8, raw_data.Length - 8);
                        break;
                }
            }

            // for (int i = 0; i < raw_data.Length; i++)
            //	System.logger.Debug ("{0} - \"{1}\"", raw_data [i].ToString ("x"), raw_data [i]);            
        }

        public byte[] GetBytes(bool is_little)
        {
            bool ascii = true;
            string description = Value;
            System.Text.Encoding enc;
            string heading;

            for (int i = 0; i < description.Length; i++)
            {
                if (description[i] > 127)
                {
                    ascii = false;
                    break;
                }
            }

            if (ascii)
            {
                heading = "ASCII\0\0\0";
                enc = new System.Text.ASCIIEncoding();
            }
            else
            {
                heading = "Unicode\0";
                enc = new System.Text.UnicodeEncoding(!is_little, true);
            }

            int len = enc.GetByteCount(description);
            byte[] data = new byte[len + heading.Length];
            System.Text.Encoding.ASCII.GetBytes(heading, 0, heading.Length, data, 0);
            enc.GetBytes(Value, 0, Value.Length, data, heading.Length);

            //UserComment c = new UserComment(data, is_little);
            //System.logger.Debug("old = \"{0}\" new = \"{1}\" heading = \"{2}\"", c.Value, description, heading);
            return data;
        }

        public override string ToString()
        {
            return String.Format("({0},charset={1})", Value, Charset);
        }
    }

    //Byte order - part of header
    //Written as either "II" (4949.H) (Intel) (little endian) or "MM" (4D4D.H) (Motorola) (big endian) depending on the CPU of the machine doing the recording.
    public enum Endian
    {
        Big,
        Little
    }


}
