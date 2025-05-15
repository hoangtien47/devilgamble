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

using static Sui.Cryptography.SignatureUtils;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Signers;
using System;
using Org.BouncyCastle.Math;

namespace Sui.Cryptography.Secp256r1
{
    /// <summary>
    /// Implements SECP256R1 public key functionality.
    /// This class handles the verification of signatures and maintains
    /// the public key in a format compatible with the Sui blockchain.
    /// </summary>
    public class PublicKey : SuiPublicKeyBase
    {
        // Store the Bouncy Castle parameters for efficient verification
        private readonly ECPublicKeyParameters _key_params;
        private readonly X9ECParameters _curve_params;
        private readonly ECDomainParameters _domain_params;

        /// <summary>
        /// Identifies this key as using the SECP256R1 signature scheme
        /// </summary>
        public override SignatureScheme SignatureScheme => SignatureScheme.Secp256r1;

        /// <summary>
        /// Specifies the length of SECP256R1 public keys (33 bytes in compressed format)
        /// </summary>
        public override int KeyLength => SignatureSchemeToSize.Secp256r1;

        /// <summary>
        /// Creates a public key from a byte array representation
        /// </summary>
        /// <param name="public_key">The public key bytes in compressed format</param>
        public PublicKey(byte[] public_key) : base(public_key)
        {
            // Initialize the curve parameters needed for verification
            _curve_params = ECNamedCurveTable.GetByName("secp256r1");
            _domain_params = new ECDomainParameters(
                _curve_params.Curve,
                _curve_params.G,
                _curve_params.N,
                _curve_params.H,
                _curve_params.GetSeed()
            );

            // Convert the raw public key bytes into Bouncy Castle parameters
            var q = _curve_params.Curve.DecodePoint(public_key);
            _key_params = new ECPublicKeyParameters(q, _domain_params);
        }

        /// <summary>
        /// Creates a public key from a string representation (hex or base64)
        /// </summary>
        /// <param name="public_key">The public key string</param>
        public PublicKey(string public_key) : base(public_key)
        {
            // Initialize curve parameters same as above
            _curve_params = ECNamedCurveTable.GetByName("secp256r1");
            _domain_params = new ECDomainParameters(
                _curve_params.Curve,
                _curve_params.G,
                _curve_params.N,
                _curve_params.H,
                _curve_params.GetSeed()
            );

            // Convert the public key bytes into Bouncy Castle parameters
            var q = _curve_params.Curve.DecodePoint(this._key_bytes);
            _key_params = new ECPublicKeyParameters(q, _domain_params);
        }

        /// <summary>
        /// Returns the flag byte that identifies this as a SECP256R1 key
        /// </summary>
        public override byte Flag() => SignatureSchemeToFlag.Secp256r1;

        /// <summary>
        /// Verifies a signature against a message using SECP256R1/ECDSA
        /// </summary>
        /// <param name="message">The original message bytes that were signed</param>
        /// <param name="signature">The signature bytes to verify</param>
        /// <returns>True if the signature is valid, false otherwise</returns>
        public override bool Verify(byte[] message, byte[] signature)
        {
            if (signature.Length != 64)
                throw new ArgumentException("Signature must be 64 bytes (compact format)");

            // Hash the message first
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

            // Use ECDSASigner directly
            var verifier = new ECDsaSigner();
            verifier.Init(false, this._key_params);

            return verifier.VerifySignature(hash, r, s);
        }
    }
}