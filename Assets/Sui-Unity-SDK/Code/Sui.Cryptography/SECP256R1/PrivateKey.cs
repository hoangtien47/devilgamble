//
//  PrivateKey.cs
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
using Chaos.NaCl;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math;

using Sui.Utilities;
using static Sui.Cryptography.SignatureUtils;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Signers;

namespace Sui.Cryptography.Secp256r1
{
    /// <summary>
    /// Implements the functionality of a SECP256R1 private key.
    /// The key may be viewed as a Hex or Base64 string as supported by Sui.
    /// </summary>
    public class PrivateKey : SuiPrivateKeyBase
    {
        // Declare and initialize the readonly fields properly
        private readonly X9ECParameters curveParameters;
        private readonly ECDomainParameters domainParameters;
        private ECPrivateKeyParameters _extended_key_params;

        /// <summary>
        /// SECP256R1 signature scheme identifier.
        /// </summary>
        public override SignatureScheme SignatureScheme => SignatureScheme.Secp256r1;

        /// <summary>
        /// Byte array representation of a SECP256R1 private key.
        /// Handles lazy initialization of the key bytes and extended parameters.
        /// </summary>
        public override byte[] KeyBytes
        {
            get
            {
                // If key bytes haven't been initialized yet, try to initialize from hex or base64
                if (this._key_bytes == null)
                {
                    if (this._key_hex != null)
                    {
                        string key = this._key_hex;
                        if (_key_hex[0..2].Equals("0x"))
                            key = this._key_hex[2..];
                        this._key_bytes = key.HexStringToByteArray();
                    }
                    else if (this._key_base64 != null)
                    {
                        string key = this._key_base64;
                        this._key_bytes = CryptoBytes.FromBase64String(key);
                    }
                }
                return this._key_bytes;
            }
            set
            {
                if (value == null)
                {
                    this.SetError<SuiError>("Private key bytes cannot be null");
                    return;
                }

                if (value.Length != this.KeyLength)
                {
                    this.SetError<SuiError>($"Invalid key length: {value.Length}");
                    return;
                }

                this._key_bytes = value;
            }
        }

        public PrivateKey(byte[] private_key) : base(private_key)
        {
            if (private_key == null || private_key.Length != this.KeyLength)
            {
                this.SetError<SuiError>($"Invalid private key");
                return;
            }

            // Initialize curve parameters in constructor
            curveParameters = ECNamedCurveTable.GetByName("secp256r1");
            domainParameters = new ECDomainParameters(
                curveParameters.Curve,
                curveParameters.G,
                curveParameters.N,
                curveParameters.H,
                curveParameters.GetSeed()
            );

            //this.SetExtendedParameters();
            InitializeExtendedParameters();
        }

        public PrivateKey(ReadOnlySpan<byte> private_key) : base(private_key)
        {
            // Initialize curve parameters in constructor
            curveParameters = ECNamedCurveTable.GetByName("secp256r1");
            domainParameters = new ECDomainParameters(
                curveParameters.Curve,
                curveParameters.G,
                curveParameters.N,
                curveParameters.H,
                curveParameters.GetSeed()
            );

            //this.SetExtendedParameters();
            InitializeExtendedParameters();
        }

        public PrivateKey(string private_key) : base(private_key)
        {
            // Initialize curve parameters in constructor
            curveParameters = ECNamedCurveTable.GetByName("secp256r1");
            domainParameters = new ECDomainParameters(
                curveParameters.Curve,
                curveParameters.G,
                curveParameters.N,
                curveParameters.H,
                curveParameters.GetSeed()
            );

            //this.SetExtendedParameters();
            // Extended parameters will be initialized when KeyBytes property is accessed
        }

        public PrivateKey() : base(PrivateKey.GetRandomSeed())
        {
            // Initialize curve parameters in constructor
            curveParameters = ECNamedCurveTable.GetByName("secp256r1");
            domainParameters = new ECDomainParameters(
                curveParameters.Curve,
                curveParameters.G,
                curveParameters.N,
                curveParameters.H,
                curveParameters.GetSeed()
            );

            //this.SetExtendedParameters();
            InitializeExtendedParameters();
        }

        /// <summary>
        /// Generates the corresponding SECP256R1 public key
        /// </summary>
        public override PublicKeyBase PublicKey()
        {
            // Check if we have an error state
            if (this.Error != null)
            {
                throw new InvalidOperationException($"Cannot generate public key: {this.Error.Message}");
            }

            // Check if we have valid key bytes
            if (this._key_bytes == null)
            {
                throw new InvalidOperationException("Private key has not been properly initialized - key bytes are null");
            }

            // If extended parameters are null, try to initialize them
            if (this._extended_key_params == null)
            {
                InitializeExtendedParameters();

                // Check if initialization failed
                if (this.Error != null)
                {
                    throw new InvalidOperationException($"Failed to initialize private key parameters: {this.Error.Message}");
                }

                if (this._extended_key_params == null)
                {
                    throw new InvalidOperationException("Failed to initialize private key parameters - parameters are null");
                }
            }

            try
            {
                var q = domainParameters.G.Multiply(this._extended_key_params.D);
                var publicKeyParams = new ECPublicKeyParameters(q, domainParameters);
                return new PublicKey(publicKeyParams.Q.GetEncoded(true));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate public key", ex);
            }
        }

        /// <summary>
        /// Helper method to safely initialize the extended key parameters
        /// </summary>
        private void InitializeExtendedParameters()
        {
            try
            {
                // Ensure we have valid key bytes
                if (this._key_bytes == null)
                {
                    this.SetError<SuiError>("Key bytes are null");
                    return;
                }

                // Create positive BigInteger from private key bytes
                var d = new BigInteger(1, this._key_bytes);

                // Validate the private key value is in the correct range
                if (d.CompareTo(BigInteger.Zero) <= 0 || d.CompareTo(domainParameters.N) >= 0)
                {
                    this.SetError<SuiError>("Private key value must be in the range [1, n-1]");
                    return;
                }

                // Create the private key parameters
                this._extended_key_params = new ECPrivateKeyParameters(d, domainParameters);
            }
            catch (Exception ex)
            {
                this.SetError<SuiError>($"Failed to initialize private key parameters: {ex.Message}");
            }
        }

        /// <summary>
        /// Signs a message using ECDSA with SHA256.
        /// </summary>
        public override SignatureBase Sign(byte[] message)
        {
            // Create SHA256 hash of message first
            var digest = new Sha256Digest();
            var hash = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(message, 0, message.Length);
            digest.DoFinal(hash, 0);

            // Use ECDSASigner directly for more control
            var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
            signer.Init(true, this._extended_key_params);

            // Generate signature
            var signature = signer.GenerateSignature(hash);
            BigInteger r = signature[0];
            BigInteger s = signature[1];

            // Enforce low S value like TypeScript implementation
            BigInteger halfN = domainParameters.N.ShiftRight(1);
            if (s.CompareTo(halfN) > 0)
                s = domainParameters.N.Subtract(s);

            // Convert to compact format (r || s)
            byte[] rBytes = r.ToByteArrayUnsigned();
            byte[] sBytes = s.ToByteArrayUnsigned();

            // Ensure both r and s are 32 bytes
            byte[] compactSignature = new byte[64];
            Array.Copy(rBytes.PadLeft(32), 0, compactSignature, 0, 32);
            Array.Copy(sBytes.PadLeft(32), 0, compactSignature, 32, 32);

            return new Signature(compactSignature);
        }

        public override SignatureBase Sign(string b64_message)
            => (Signature)this.Sign(CryptoBytes.FromBase64String(b64_message));

        /// <summary>
        /// Generates a random seed for the SECP256R1 private key.
        /// </summary>
        private static byte[] GetRandomSeed()
        {
            var secureRandom = new SecureRandom();
            byte[] seed = new byte[32]; // SECP256R1 uses 32-byte private keys
            secureRandom.NextBytes(seed);
            return seed;
        }
    }
}