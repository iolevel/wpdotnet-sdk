using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PeachPied.WordPress.Standard.Internal
{
    static class Keys
    {

        /// <summary>
        /// Gets our private key.
        /// </summary>
        static RSAParameters PublicKey
        {
            get
            {
                if (!_publicKey.HasValue)
                {
                    _publicKey = JsonSerializer.Deserialize<RSAParameters>(Resources.Resource.PublicKey, new JsonSerializerOptions
                    {
                        IncludeFields = true,
                    });
                }

                return _publicKey.Value;
            }
        }

        static RSAParameters? _publicKey;

        public static byte[] EncryptData(byte[] bytes)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(PublicKey);
                return rsa.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
            }
        }

        public static bool VerifyData(byte[]/*!*/data, byte[] signature)
        {
            if (data == null) throw new ArgumentException();

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(PublicKey);
                return rsa.VerifyData(data, SHA256.Create(), signature);
            }
        }
    }
}
