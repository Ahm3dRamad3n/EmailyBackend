using Emaily.BLL.Helpers.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Emaily.BLL.Helpers.Services
{
    public class EncryptionHelper(IConfiguration configuration) : IEncryptionHelper
    {
        private readonly string secret = configuration["ENCRYPTION_MASTER_KEY"]
                            ?? throw new Exception("ENCRYPTION_MASTER_KEY is missing in .env");

        /// <summary>
        /// تم تحديث الدالة لاستخدام AES-GCM (Authenticated Encryption)
        /// (تم الاحتفاظ باسم الدالة القديم لعدم كسر معمارية النظام)
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // أحجام قياسية لخوارزمية AES-GCM
            int nonceSize = AesGcm.NonceByteSizes.MaxSize; // 12 bytes
            int tagSize = AesGcm.TagByteSizes.MaxSize;     // 16 bytes

            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[tagSize];
            byte[] cipherBytes = new byte[plainBytes.Length];

            // توليد IV (Nonce) عشوائي آمن
            RandomNumberGenerator.Fill(nonce);

            using (var aesGcm = new AesGcm(SHA256.HashData(Encoding.UTF8.GetBytes(secret)), tagSizeInBytes: 16))
            {
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // دمج (Nonce + Tag + Cipher) في مصفوفة واحدة لسهولة الحفظ
            byte[] encryptedData = new byte[nonceSize + tagSize + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, encryptedData, 0, nonceSize);
            Buffer.BlockCopy(tag, 0, encryptedData, nonceSize, tagSize);
            Buffer.BlockCopy(cipherBytes, 0, encryptedData, nonceSize + tagSize, cipherBytes.Length);

            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// فك التشفير مع التحقق من سلامة البيانات (Integrity Check)
        /// </summary>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            byte[] encryptedData = Convert.FromBase64String(cipherText);

            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;
            int cipherSize = encryptedData.Length - nonceSize - tagSize;

            if (cipherSize < 0)
                throw new CryptographicException("Invalid encrypted data format.");

            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[tagSize];
            byte[] cipherBytes = new byte[cipherSize];
            byte[] plainBytes = new byte[cipherSize];

            // فصل الأجزاء الثلاثة من المصفوفة المدمجة
            Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonceSize);
            Buffer.BlockCopy(encryptedData, nonceSize, tag, 0, tagSize);
            Buffer.BlockCopy(encryptedData, nonceSize + tagSize, cipherBytes, 0, cipherSize);

            using (var aesGcm = new AesGcm(SHA256.HashData(Encoding.UTF8.GetBytes(secret)), tagSizeInBytes: 16))
            {
                // إذا تم تغيير حرف واحد في الداتابيز، هذه الدالة سترفض العمل وترمي Exception للحماية
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}