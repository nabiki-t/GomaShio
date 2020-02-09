using System;
using System.Linq;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace GomaShio
{
    internal static class Crypt
    {
        private static int HASH_ITER_COUNT = 8192;

        public static byte[] CipherEncryption(
            string plainText,
            string password )
        {
            // Convert to password string to UTF-8 encoded binary data.
            IBuffer utf8Password = CryptographicBuffer.ConvertStringToBinary( password, BinaryStringEncoding.Utf8 );

            // Generate hash value of password string.
            IBuffer salt1 = CryptographicBuffer.GenerateRandom( 32 );
            CryptographicHash hashObj =
                HashAlgorithmProvider.OpenAlgorithm( HashAlgorithmNames.Sha512 ).CreateHash();
            hashObj.Append( salt1 );
            hashObj.Append( utf8Password );
            IBuffer buffHash1 = hashObj.GetValueAndReset();
            for ( int i = 0; i < HASH_ITER_COUNT; i++ ) {
                hashObj.Append( buffHash1 );
                buffHash1 = hashObj.GetValueAndReset();
            }
            byte[] d = buffHash1.ToArray();

            // Generate one-time password
            SymmetricKeyAlgorithmProvider objAlg = SymmetricKeyAlgorithmProvider.OpenAlgorithm( SymmetricAlgorithmNames.AesCbcPkcs7 );
            IBuffer oneTimePass = CryptographicBuffer.GenerateRandom( 64 );

            // encrypt one-time password by specified password string
            IBuffer salt2 = CryptographicBuffer.GenerateRandom( 32 );
            CryptographicKey derviedKey =
                objAlg.CreateSymmetricKey(
                    CryptographicEngine.DeriveKeyMaterial(
                        KeyDerivationAlgorithmProvider.OpenAlgorithm(
                            KeyDerivationAlgorithmNames.Pbkdf2Sha256
                        ).CreateKey( utf8Password ),
                        KeyDerivationParameters.BuildForPbkdf2( salt2, 32768 ),
                        32
                    )
                );
            IBuffer iv2 = CryptographicBuffer.GenerateRandom( objAlg.BlockLength );
            IBuffer encryptOneTimePass = CryptographicEngine.Encrypt( derviedKey, oneTimePass, iv2 );

            // Encrypt the data
            IBuffer iv3 = CryptographicBuffer.GenerateRandom( objAlg.BlockLength );
            IBuffer encryptData =
                CryptographicEngine.Encrypt(
                    objAlg.CreateSymmetricKey( oneTimePass ),
                    CryptographicBuffer.ConvertStringToBinary( plainText, BinaryStringEncoding.Utf8 ),
                    iv3
            );

            // Append all of data
            byte[] retval =
                new byte[
                    146 +
                    (int)objAlg.BlockLength +
                    (int)objAlg.BlockLength + 
                    encryptOneTimePass.Length +
                    encryptData.Length
                ];
            int pos = 0;
            Array.Copy( Encoding.GetEncoding( "ASCII" ).GetBytes( "GomaShio" ), 0, retval, pos, 8 );
            pos += 8;
            Array.Copy( new byte[2]{ 0, 0 },            0, retval, pos, 2 );
            pos += 2;
            Array.Copy( salt1.ToArray(),                0, retval, pos, 32 );
            pos += 32;
            Array.Copy( buffHash1.ToArray(),            0, retval, pos, 64 );
            pos += 64;
            Array.Copy( salt2.ToArray(),                0, retval, pos, 32 );
            pos += 32;
            Array.Copy( iv2.ToArray(),                  0, retval, pos, objAlg.BlockLength );
            pos += (int)objAlg.BlockLength;
            Array.Copy( BitConverter.GetBytes( encryptOneTimePass.Length ), 0, retval, pos, 4 );
            pos += 4;
            Array.Copy( encryptOneTimePass.ToArray(),   0, retval, pos, encryptOneTimePass.Length );
            pos += (int)encryptOneTimePass.Length;
            Array.Copy( iv3.ToArray(),                  0, retval, pos, objAlg.BlockLength );
            pos += (int)objAlg.BlockLength;
            Array.Copy( BitConverter.GetBytes( encryptData.Length ), 0, retval, pos, 4 );
            pos += 4;
            Array.Copy( encryptData.ToArray(),          0, retval, pos, encryptData.Length );

            return retval;
        }

        public static bool CipherDecryption(
            byte[] encData,
            string password,
            out string plainText,
            out bool passwordError)
        {
            plainText = "";
            passwordError = false;

            CryptographicHash hashObj =
                HashAlgorithmProvider.OpenAlgorithm( HashAlgorithmNames.Sha512 ).CreateHash();
            SymmetricKeyAlgorithmProvider objAlg =
                SymmetricKeyAlgorithmProvider.OpenAlgorithm( SymmetricAlgorithmNames.AesCbcPkcs7 );

            if ( encData.Length < 146 + ( objAlg.BlockLength * 2 ) )
                return false;

            byte[] MagicBytes   = new byte[8];
            byte[] Version      = new byte[2];
            byte[] solt1        = new byte[32];
            byte[] hash         = new byte[64];
            byte[] solt2        = new byte[32];
            byte[] iv2          = new byte[objAlg.BlockLength];
            byte[] iv3          = new byte[objAlg.BlockLength];

            // Split encripted data
            int pos = 0;
            Array.Copy( encData, pos, MagicBytes, 0, 8 );
            pos += 8;

            Array.Copy( encData, pos, Version,    0, 2 );
            pos += 2;

            Array.Copy( encData, pos, solt1,      0, 32 );
            pos += 32;

            Array.Copy( encData, pos, hash,       0, 64 );
            pos += 64;

            Array.Copy( encData, pos, solt2,      0, 32 );
            pos += 32;

            Array.Copy( encData, pos, iv2,        0, objAlg.BlockLength );
            pos += (int)objAlg.BlockLength;

            int oneTimePassLength = BitConverter.ToInt32( encData, pos );
            pos += 4;
            if ( encData.Length < pos + oneTimePassLength ) return false;

            byte[] oneTimePass  = new byte[oneTimePassLength];
            Array.Copy( encData, pos,   oneTimePass, 0, oneTimePassLength );
            pos += oneTimePassLength;
            if ( encData.Length < pos + objAlg.BlockLength ) return false;

            Array.Copy( encData, pos,   iv3,        0, objAlg.BlockLength );
            pos += (int)objAlg.BlockLength;
            if ( encData.Length < pos + 4 ) return false;

            int encDataSubLength = BitConverter.ToInt32( encData, pos );
            pos += 4;
            if ( encData.Length < pos + encDataSubLength ) return false;

            byte[] encDataSub  = new byte[encDataSubLength];
            Array.Copy( encData, pos,   encDataSub, 0, encDataSubLength );

            // Check magic bytes and version.
            if ( !Enumerable.SequenceEqual( MagicBytes, Encoding.GetEncoding( "ASCII" ).GetBytes( "GomaShio" ) ) )
                return false;
            if ( !Enumerable.SequenceEqual( Version, new byte[2]{ 0, 0 } ) )
                return false;

            // Check password
            IBuffer utf8Password = CryptographicBuffer.ConvertStringToBinary( password, BinaryStringEncoding.Utf8 );
            IBuffer salt1 = solt1.AsBuffer();
            hashObj.Append( salt1 );
            hashObj.Append( utf8Password );
            IBuffer buffHash1 = hashObj.GetValueAndReset();
            for ( int i = 0; i < HASH_ITER_COUNT; i++ ) {
                hashObj.Append( buffHash1 );
                buffHash1 = hashObj.GetValueAndReset();
            }
            if ( !Enumerable.SequenceEqual( hash, buffHash1.ToArray() ) ) {
                passwordError = true;
                return false;
            }

            // Decrypt one-time password by specified password string
            CryptographicKey derviedKey =
                objAlg.CreateSymmetricKey(
                    CryptographicEngine.DeriveKeyMaterial(
                        KeyDerivationAlgorithmProvider.OpenAlgorithm(
                            KeyDerivationAlgorithmNames.Pbkdf2Sha256
                        ).CreateKey( utf8Password ),
                        KeyDerivationParameters.BuildForPbkdf2( solt2.AsBuffer(), 32768 ),
                        32
                    )
                );
            IBuffer plainOneTimePass = CryptographicEngine.Decrypt( derviedKey, oneTimePass.AsBuffer(), iv2.AsBuffer() );

            // Decrypt the data
            plainText =
                CryptographicBuffer.ConvertBinaryToString(
                    BinaryStringEncoding.Utf8,
                    CryptographicEngine.Decrypt(
                        objAlg.CreateSymmetricKey( plainOneTimePass ),
                        encDataSub.AsBuffer(),
                        iv3.AsBuffer()
                    )
                );

            return true;
        }
    }
}
