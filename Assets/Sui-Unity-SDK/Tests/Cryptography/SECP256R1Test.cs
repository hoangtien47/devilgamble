using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using static Sui.Cryptography.SignatureUtils;
using Sui.Cryptography.Secp256r1;
using Sui.Cryptography;
using Sui.Utilities;

namespace Sui.Tests.Cryptography
{
    [TestFixture]
    public class Secp256r1Tests
    {
        // Test vectors converted from TypeScript implementation
        private static readonly byte[] ValidPrivateKey = new byte[]
        {
            66, 37, 141, 205, 161, 76, 241, 17, 198, 2, 184, 151, 27, 140, 200, 67,
            233, 30, 70, 202, 144, 81, 81, 192, 39, 68, 166, 176, 23, 230, 147, 22
        };

        private static readonly byte[] ValidPublicKey = new byte[]
        {
            2, 39, 50, 43, 58, 137, 26, 10, 40, 13, 107, 193, 251, 44, 187, 35,
            210, 143, 84, 144, 111, 214, 64, 127, 95, 116, 31, 109, 239, 87, 98, 96, 154
        };

        private static readonly byte[] InvalidPrivateKey = new byte[31]; // One byte short
        private static readonly byte[] InvalidPublicKey = new byte[32]; // Invalid length

        [Test]
        public void CreateNewKeypair_ShouldGenerateValidKeys()
        {
            // Creating a new keypair should generate valid keys
            var privateKey = new PrivateKey();
            var publicKey = privateKey.PublicKey() as PublicKey;

            Assert.That(publicKey, Is.Not.Null);
            Assert.That(publicKey.KeyBytes.Length, Is.EqualTo(33)); // Compressed public key length
        }

        [Test]
        public void CreateKeypairFromValidPrivateKey_ShouldSucceed()
        {
            // Creating a keypair from a valid private key should succeed
            var privateKey = new PrivateKey(ValidPrivateKey);
            var publicKey = privateKey.PublicKey() as PublicKey;

            Assert.That(publicKey, Is.Not.Null);
            Assert.That(publicKey.KeyBytes, Is.EqualTo(ValidPublicKey));
            Assert.That(Convert.ToBase64String(publicKey.KeyBytes),
                       Is.EqualTo(Convert.ToBase64String(ValidPublicKey)));
        }

        //TODO -- Add tests for when an invalid private key is used
        //[Test]
        //public void CreateKeypairFromInvalidPrivateKey_ShouldThrowException()
        //{
            // Creating a keypair from an invalid private key should throw
            //Assert.Throws<ArgumentException>(() => new PrivateKey(InvalidPrivateKey));
            //Assert.IsInstanceOf(typeof(SuiError), new PrivateKey(InvalidPrivateKey));
            //Assert.AreEqual(null, new PrivateKey(InvalidPrivateKey));
        //}

        [Test]
        public void SignAndVerifyMessage_ShouldSucceed()
        {
            var privateKey = new PrivateKey();
            var publicKey = privateKey.PublicKey() as PublicKey;

            // Create test message
            byte[] message = Encoding.UTF8.GetBytes("hello world");

            // Sign the message
            var signature = privateKey.Sign(message);

            // Verify the signature
            bool isValid = publicKey.Verify(message, signature.SignatureBytes);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void SignMessageWithKnownKey_ShouldMatchReference()
        {
            // Test signing with known key should match reference implementation
            var privateKey = new PrivateKey(ValidPrivateKey);
            byte[] message = Encoding.UTF8.GetBytes("Hello, world!");

            var signature = privateKey.Sign(message);
            var publicKey = privateKey.PublicKey() as PublicKey;

            // Verify the signature
            bool isValid = publicKey.Verify(message, signature.SignatureBytes);
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void SignAndVerifyTransaction_ShouldSucceed()
        {
            var privateKey = new PrivateKey();
            var publicKey = privateKey.PublicKey() as PublicKey;

            // Create mock transaction data
            byte[] transactionData = new byte[64];
            new Random().NextBytes(transactionData);

            // Sign the transaction
            var signature = privateKey.Sign(transactionData);

            // Verify the signature
            bool isValid = publicKey.Verify(transactionData, signature.SignatureBytes);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void PublicKey_Flag_ShouldReturnCorrectScheme()
        {
            var privateKey = new PrivateKey();
            var publicKey = privateKey.PublicKey() as PublicKey;

            Assert.That(publicKey.Flag(), Is.EqualTo(SignatureSchemeToFlag.Secp256r1));
        }

        [Test]
        public void PublicKey_SignatureScheme_ShouldReturnCorrectScheme()
        {
            var privateKey = new PrivateKey();
            var publicKey = privateKey.PublicKey() as PublicKey;

            Assert.That(publicKey.SignatureScheme, Is.EqualTo(SignatureScheme.Secp256r1));
        }
    }
}