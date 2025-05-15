using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Sui.Cryptography;
using Sui.Cryptography.Secp256k1;
using Org.BouncyCastle.Crypto.Digests;
using UnityEngine;
using Sui.Accounts;

namespace Sui.Tests.Cryptography
{
    [TestFixture]
    public class Secp256k1PublicKeyTests
    {
        // Test case from TypeScript implementation
        private static readonly byte[] VALID_SECP256K1_PUBLIC_KEY = new byte[]
        {
            2, 29, 21, 35, 7, 198, 183, 43, 14, 208, 65, 139, 14, 112, 205, 128,
            231, 245, 41, 91, 141, 134, 245, 114, 45, 63, 82, 19, 251, 210, 57, 79, 54
        };

        // Invalid public key with incorrect length
        private static readonly byte[] INVALID_SECP256K1_PUBLIC_KEY = Enumerable.Repeat((byte)1, 32).ToArray();

        // Test cases generated against CLI
        private static readonly TestCase[] TEST_CASES = new[]
        {
            new TestCase
            {
                RawPublicKey = "AwTC3jVFRxXc3RJIFgoQcv486QdqwYa8vBp4bgSq0gsI",
                SuiPublicKey = "AQMEwt41RUcV3N0SSBYKEHL+POkHasGGvLwaeG4EqtILCA==",
                SuiAddress = "0xcdce00b4326fb908fdac83c35bcfbda323bfcc0618b47c66ccafbdced850efaa"
            },
            new TestCase
            {
                RawPublicKey = "A1F2CtldIGolO92Pm9yuxWXs5E07aX+6ZEHAnSuKOhii",
                SuiPublicKey = "AQNRdgrZXSBqJTvdj5vcrsVl7ORNO2l/umRBwJ0rijoYog==",
                SuiAddress = "0xb588e58ed8967b6a6f9dbce76386283d374cf7389fb164189551257e32b023b2"
            },
            new TestCase
            {
                RawPublicKey = "Ak5rsa5Od4T6YFN/V3VIhZ/azMMYPkUilKQwc+RiaId+",
                SuiPublicKey = "AQJOa7GuTneE+mBTf1d1SIWf2szDGD5FIpSkMHPkYmiHfg==",
                SuiAddress = "0x694dd74af1e82b968822a82fb5e315f6d20e8697d5d03c0b15e0178c1a1fcfa0"
            },
            new TestCase
            {
                RawPublicKey = "A4XbJ3fLvV/8ONsnLHAW1nORKsoCYsHaXv9FK1beMtvY",
                SuiPublicKey = "AQOF2yd3y71f/DjbJyxwFtZzkSrKAmLB2l7/RStW3jLb2A==",
                SuiAddress = "0x78acc6ca0003457737d755ade25a6f3a144e5e44ed6f8e6af4982c5cc75e55e7"
            }
        };

        public class TestCase
        {
            public string RawPublicKey { get; set; }
            public string SuiPublicKey { get; set; }
            public string SuiAddress { get; set; }
        }

        // TODO: Implement test for invalid keys
        //[Test]
        //public void Invalid_PublicKey_ThrowsException()
        //{
        //    // Test with invalid byte array
        //    Assert.Throws<ArgumentException>(() =>
        //        new PublicKey(INVALID_SECP256K1_PUBLIC_KEY));

        //    // Test with invalid base64
        //    Assert.Throws<ArgumentException>(() =>
        //        new PublicKey(Convert.ToBase64String(INVALID_SECP256K1_PUBLIC_KEY)));

        //    // Test with invalid hex string of valid key
        //    Assert.Throws<ArgumentException>(() =>
        //        new PublicKey(BitConverter.ToString(VALID_SECP256K1_PUBLIC_KEY).Replace("-", "")));

        //    // Test with invalid string
        //    Assert.Throws<ArgumentException>(() =>
        //        new PublicKey("12345"));
        //}

        [Test]
        public void ToSuiAddress_FromBase64PublicKey_Debug()
        {
            // Test with first test case
            var testCase = TEST_CASES[0];

            Debug.Log($"Input RawPublicKey: {testCase.RawPublicKey}");
            Debug.Log($"Expected SuiAddress: {testCase.SuiAddress}");

            var key = new PublicKey(testCase.RawPublicKey);
            Assert.That(key, Is.Not.Null, "PublicKey should not be null");
            Assert.That(key.KeyBytes, Is.Not.Null, "KeyBytes should not be null");

            Debug.Log($"KeyBytes length: {key.KeyBytes.Length}");
            Debug.Log($"KeyBytes hex: {BitConverter.ToString(key.KeyBytes)}");

            var suiBytes = key.ToSuiBytes();
            Debug.Log($"SuiBytes length: {suiBytes.Length}");
            Debug.Log($"SuiBytes hex: {BitConverter.ToString(suiBytes)}");

            // Create Blake2b hash manually to verify
            var blake2b = new Blake2bDigest(256);
            var hashedAddress = new byte[32]; // Use 32 bytes for 256-bit hash
            blake2b.BlockUpdate(suiBytes, 0, suiBytes.Length);
            blake2b.DoFinal(hashedAddress, 0);

            Debug.Log($"Hashed address hex: {BitConverter.ToString(hashedAddress).Replace("-", "").ToLowerInvariant()}");

            //var suiAddress = key.ToSuiAddress();
            //Debug.Log($"Final SuiAddress: {suiAddress}");

            //Assert.That(suiAddress, Is.Not.Null, "SuiAddress should not be null");
            //Assert.That(suiAddress.ToString(), Is.EqualTo(testCase.SuiAddress));

            var hashHex = BitConverter.ToString(hashedAddress).Replace("-", "").ToLowerInvariant();
            Debug.Log($"Hashed address hex: {hashHex}");

            // Try creating hex string with 0x prefix
            var addressHex = "0x" + hashHex;
            Debug.Log($"Address hex with prefix: {addressHex}");

            try
            {
                var suiAddress = AccountAddress.FromHex(addressHex);
                Debug.Log($"Successfully created AccountAddress: {suiAddress}");
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to create AccountAddress. Exception: {ex.GetType().Name}: {ex.Message}");
                Debug.Log($"Stack trace: {ex.StackTrace}");
            }
        }

        [Test]
        public void ToBase64_ReturnsCorrectString()
        {
            var pubKeyBase64 = Convert.ToBase64String(VALID_SECP256K1_PUBLIC_KEY);
            var key = new PublicKey(pubKeyBase64);
            Assert.That(key.ToBase64(), Is.EqualTo(pubKeyBase64), "BASE64: " + key.ToBase64());
        }

        [Test]
        public void ToRawBytes_ReturnsCorrectBytes()
        {
            var pubKeyBase64 = Convert.ToBase64String(VALID_SECP256K1_PUBLIC_KEY);
            var key = new PublicKey(pubKeyBase64);

            Assert.That(key.KeyBytes.Length, Is.EqualTo(33), "BYTES LENGTH: " + key.KeyBytes.Length);

            var newKey = new PublicKey(key.KeyBytes);
            Assert.That(newKey.Equals(key), Is.True, "NEW KEY BYTES: " + string.Join(",", key.KeyBytes));
        }

        [TestCaseSource(nameof(TEST_CASES))]
        public void ToSuiAddress_FromBase64PublicKey_ReturnsCorrectAddress(TestCase testCase)
        {
            var key = new PublicKey(testCase.RawPublicKey);
            Assert.That(key.ToSuiAddress().ToString(), Is.EqualTo(testCase.SuiAddress));
        }

        [TestCaseSource(nameof(TEST_CASES))]
        public void ToSuiPublicKey_FromBase64PublicKey_ReturnsCorrectKey(TestCase testCase)
        {
            var key = new PublicKey(testCase.RawPublicKey);
            Assert.That(key.ToSuiPublicKey(), Is.EqualTo(testCase.SuiPublicKey));
        }

        [Test]
        public void SignAndVerify_ValidMessage_Succeeds()
        {
            // Create keypair and get public key
            var privateKey = new PrivateKey();
            var publicKey = privateKey.PublicKey() as PublicKey;

            // Sign message
            var message = System.Text.Encoding.UTF8.GetBytes("hello world");
            var signature = privateKey.Sign(message);

            // Verify signature
            Assert.That(publicKey.Verify(message, signature.SignatureBytes), Is.True);
        }

        [Test]
        public void SignAndVerify_ModifiedMessage_Fails()
        {
            // Create keypair and get public key
            var privateKey = new PrivateKey();
            var publicKey = privateKey.PublicKey() as PublicKey;

            // Sign original message
            var message = System.Text.Encoding.UTF8.GetBytes("hello world");
            var signature = privateKey.Sign(message);

            // Modify message
            var modifiedMessage = System.Text.Encoding.UTF8.GetBytes("hello world!");

            // Verify should fail with modified message
            Assert.That(publicKey.Verify(modifiedMessage, signature.SignatureBytes), Is.False);
        }
    }
}