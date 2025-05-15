using System;
using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Sui.Utilities;
using UnityEngine;
using static Sui.Cryptography.SignatureUtils;

namespace Sui.Cryptography.Secp256k1
{
    public class PrivateKey : SuiPrivateKeyBase
    {
        private readonly X9ECParameters curveParameters;
        private readonly ECDomainParameters domainParameters;
        private ECPrivateKeyParameters _extended_key_params;
        private const string DEFAULT_SECP256K1_DERIVATION_PATH = "m/54'/784'/0'/0/0";

        public override SignatureScheme SignatureScheme => SignatureScheme.Secp256k1;

        public override byte[] KeyBytes
        {
            get
            {
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
                        this._key_bytes = Convert.FromBase64String(key);
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

            curveParameters = ECNamedCurveTable.GetByName("secp256k1");
            domainParameters = new ECDomainParameters(
                curveParameters.Curve,
                curveParameters.G,
                curveParameters.N,
                curveParameters.H,
                curveParameters.GetSeed()
            );

            InitializeExtendedParameters();
        }

        public PrivateKey() : this(GetRandomSeed()) { }

        public static PrivateKey FromSecretKey(byte[] secretKey, bool skipValidation = false)
        {
            var privateKey = new PrivateKey(secretKey);

            if (!skipValidation)
            {
                // Validate using the same pattern as TypeScript
                var signData = System.Text.Encoding.UTF8.GetBytes("sui validation");
                var blake2b = new Blake2bDigest(256);
                var msgHash = new byte[blake2b.GetDigestSize()];
                blake2b.BlockUpdate(signData, 0, signData.Length);
                blake2b.DoFinal(msgHash, 0);

                try
                {
                    var publicKey = privateKey.PublicKey();
                    var signature = privateKey.Sign(signData);
                    if (!publicKey.Verify(signData, signature.SignatureBytes))
                    {
                        throw new ArgumentException("Provided secretKey is invalid");
                    }
                }
                catch
                {
                    throw new ArgumentException("Provided secretKey is invalid");
                }
            }

            return privateKey;
        }

        public static PrivateKey FromSeed(byte[] seed)
        {
            return new PrivateKey(seed);
        }

        public override PublicKeyBase PublicKey()
        {
            if (this.Error != null)
            {
                throw new InvalidOperationException($"Cannot generate public key: {this.Error.Message}");
            }

            if (this._extended_key_params == null)
            {
                InitializeExtendedParameters();
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

        private void InitializeExtendedParameters()
        {
            try
            {
                if (this._key_bytes == null)
                {
                    this.SetError<SuiError>("Key bytes are null");
                    return;
                }

                var d = new BigInteger(1, this._key_bytes);

                if (d.CompareTo(BigInteger.One) < 0 || d.CompareTo(domainParameters.N) >= 0)
                {
                    this.SetError<SuiError>("Private key value must be in the range [1, n-1]");
                    return;
                }

                this._extended_key_params = new ECPrivateKeyParameters(d, domainParameters);
            }
            catch (Exception ex)
            {
                this.SetError<SuiError>($"Failed to initialize private key parameters: {ex.Message}");
            }
        }

        public override SignatureBase Sign(byte[] message)
        {
            // Create SHA256 hash of message to match TypeScript implementation
            var sha256 = new Sha256Digest();
            var hash = new byte[sha256.GetDigestSize()];
            sha256.BlockUpdate(message, 0, message.Length);
            sha256.DoFinal(hash, 0);

            // Use deterministic K calculation
            var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
            signer.Init(true, this._extended_key_params);

            BigInteger[] signature = signer.GenerateSignature(hash);
            BigInteger r = signature[0];
            BigInteger s = signature[1];

            // Enforce low S value
            BigInteger halfN = domainParameters.N.ShiftRight(1);
            if (s.CompareTo(halfN) > 0)
            {
                s = domainParameters.N.Subtract(s);
            }

            // Convert to compact format (r || s) - matching TypeScript implementation
            byte[] rBytes = r.ToByteArrayUnsigned();
            byte[] sBytes = s.ToByteArrayUnsigned();

            byte[] compactSignature = new byte[64];
            Array.Copy(rBytes.PadLeft(32), 0, compactSignature, 0, 32);
            Array.Copy(sBytes.PadLeft(32), 0, compactSignature, 32, 32);

            return new Signature(compactSignature);
        }

        private static byte[] GetRandomSeed()
        {
            var secureRandom = new SecureRandom();
            byte[] seed = new byte[32];
            secureRandom.NextBytes(seed);
            return seed;
        }

        //public static PrivateKey DeriveKeypair(string mnemonics, string path = null)
        //{
        //    path = path ?? DEFAULT_SECP256K1_DERIVATION_PATH;

        //    if (!IsValidBIP32Path(path))
        //    {
        //        throw new ArgumentException("Invalid derivation path");
        //    }

        //    var seed = MnemonicToSeed(mnemonics);
        //    var key = GetHDKeyFromSeed(seed).DerivePath(path);

        //    if (key.PrivateKey == null || key.PublicKey == null)
        //    {
        //        throw new InvalidOperationException("Invalid key");
        //    }

        //    return new PrivateKey(key.PrivateKey);
        //}

        public override SignatureBase Sign(string b64_message)
        {
            throw new NotImplementedException();
        }
    }
}