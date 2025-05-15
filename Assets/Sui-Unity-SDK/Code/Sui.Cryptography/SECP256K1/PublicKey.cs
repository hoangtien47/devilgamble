//
//  PublicKey.cs
//  Sui-Unity-SDK
//
//  Copyright (c) 2025 OpenDive
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//

using System;
using System.Linq;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using static Sui.Cryptography.SignatureUtils;

namespace Sui.Cryptography.Secp256k1
{
    public class PublicKey : SuiPublicKeyBase
    {
        private readonly ECPublicKeyParameters _publicKeyParams;

        public override int KeyLength { get => SignatureSchemeToSize.Secp256k1; }
        public override SignatureScheme SignatureScheme => SignatureScheme.Secp256k1;

        public PublicKey(byte[] publicKey) : base(publicKey)
        {
            if (publicKey.Length != KeyLength)
            {
                throw new ArgumentException(
                    $"Invalid public key input. Expected {KeyLength} bytes, got {publicKey.Length}");
            }

            var curve = ECNamedCurveTable.GetByName("secp256k1");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

            try
            {
                var q = curve.Curve.DecodePoint(publicKey);
                _publicKeyParams = new ECPublicKeyParameters(q, domainParams);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid public key", nameof(publicKey), ex);
            }
        }

        public PublicKey(string publicKey) : base(publicKey)
        {
            byte[] keyBytes = this.KeyBytes; // This will initialize from the base class
            if (keyBytes == null)
            {
                throw new ArgumentException("Failed to initialize key bytes from string");
            }

            if (keyBytes.Length != KeyLength)
            {
                throw new ArgumentException(
                    $"Invalid public key input. Expected {KeyLength} bytes, got {keyBytes.Length}");
            }

            var curve = ECNamedCurveTable.GetByName("secp256k1");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

            try
            {
                var q = curve.Curve.DecodePoint(keyBytes);
                _publicKeyParams = new ECPublicKeyParameters(q, domainParams);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid public key", nameof(publicKey), ex);
            }
        }

        public override bool Verify(byte[] message, byte[] signature)
        {
            if (signature.Length != 64)
            {
                throw new ArgumentException("Signature must be 64 bytes (compact format)");
            }

            // Hash with SHA256 to match TypeScript implementation
            var digest = new Sha256Digest();
            var hash = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(message, 0, message.Length);
            digest.DoFinal(hash, 0);

            // Split signature into r and s
            byte[] rBytes = new byte[32];
            byte[] sBytes = new byte[32];
            Array.Copy(signature, 0, rBytes, 0, 32);
            Array.Copy(signature, 32, sBytes, 0, 32);

            var r = new BigInteger(1, rBytes);
            var s = new BigInteger(1, sBytes);

            var verifier = new ECDsaSigner();
            verifier.Init(false, this._publicKeyParams);

            return verifier.VerifySignature(hash, r, s);
        }

        public override byte Flag() => SignatureSchemeToFlag.Secp256k1;
    }
}