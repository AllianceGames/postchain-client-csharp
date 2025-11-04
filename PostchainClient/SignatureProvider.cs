using NBitcoin.Secp256k1;
using System;
using System.Linq;

namespace Chromia
{
    /// <summary>
    /// Wrapper for a Secp256k1 key pair.
    /// </summary>
    public readonly struct KeyPair
    {
        /// <summary>
        /// Public key of the keypair.
        /// </summary>
        public readonly Buffer PubKey;

        /// <summary>
        /// Privat key of the keypair.
        /// </summary>
        public readonly Buffer PrivKey;

        /// <summary>
        /// Creates a key pair out of the given <paramref name="privKey"/>.
        /// </summary>
        /// <param name="privKey">Buffer containing the private key. Has to be 32 bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public KeyPair(Buffer privKey)
        {
            PrivKey = privKey;
            PubKey = GetPubKey(privKey);
        }

        /// <summary>
        /// Generates a new random private key.
        /// </summary>
        /// <returns>The <see cref="Buffer"/> containing the private key.</returns>
        public static Buffer GeneratePrivKey()
        {
            var rnd = new Random();
            var valid = "0123456789abcdef";
            var privKey = Enumerable.Range(0, 64).Aggregate("", (c, _) => c + valid[rnd.Next(valid.Length)]);
            return Buffer.From(privKey);
        }

        /// <summary>
        /// Generates a public key out of the given <paramref name="privKey"/>.
        /// </summary>
        /// <param name="privKey">Buffer containing the private key. Has to be 32 bytes.</param>
        /// <returns>The <see cref="Buffer"/> containing the public key.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Buffer GetPubKey(Buffer privKey)
        {
            var privateKey = ECPrivKey.Create(privKey.Bytes);
            return Buffer.From(privateKey.CreatePubKey().ToBytes());
        }

        /// <summary>
        /// Ensures that a buffer is a valid public key.
        /// </summary>
        /// <param name="pubKey">The buffer to check for a public key.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void EnsurePublicKey(Buffer pubKey)
        {
            if (pubKey.Length != 33)
                throw new ArgumentOutOfRangeException(nameof(pubKey), "has to be 33 bytes");
        }

        /// <summary>
        /// Ensures that a buffer is a valid private key.
        /// </summary>
        /// <param name="privKey">The buffer to check for a private key.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void EnsurePrivateKey(Buffer privKey)
        {
            if (privKey.Length != 32)
                throw new ArgumentOutOfRangeException(nameof(privKey), "has to be 32 bytes");
        }
    }

    /// <summary>
    /// Wrapper for a signed message hash.
    /// </summary>
    public readonly struct Signature
    {
        /// <summary>
        /// Public key of the key pair that signed created the <see cref="Hash"/>.
        /// </summary>
        public readonly Buffer PubKey;

        /// <summary>
        /// Signature hash of a message.
        /// </summary>
        public readonly Buffer Hash;

        /// <summary>
        /// Creates a new signature.
        /// </summary>
        /// <param name="pubKey">The public key of the key pair that signed the <paramref name="hash"/>. Has to be a valid public key.</param>
        /// <param name="hash">Signature hash of a message.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Signature(Buffer pubKey, Buffer hash)
        {
            KeyPair.EnsurePublicKey(pubKey);
            Ensure(hash);

            PubKey = pubKey;
            Hash = hash;
        }

        /// <summary>
        /// Ensures that a buffer is a valid signature hash.
        /// </summary>
        /// <param name="hash">The buffer to check for a signature hash.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void Ensure(Buffer hash)
        {
            if (hash.Length != 64)
                throw new ArgumentOutOfRangeException(nameof(hash), "has to be 64 bytes");
        }
    }

    /// <summary>
    /// Provides a key pair to sign transactions.
    /// </summary>
    public interface ISignatureProvider
    {
        /// <summary>
        /// Public key of the keypair used by the signature provider.
        /// </summary>
        Buffer PubKey { get; }

        /// <summary>
        /// Signs the given <see cref="Buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to sign.</param>
        /// <returns>The signed hash wrapped as a <see cref="Signature"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        Signature Sign(Buffer buffer);
    }

    /// <inheritdoc/>
    public class SignatureProvider : ISignatureProvider
    {
        /// <inheritdoc/>
        public Buffer PubKey => _keyPair.PubKey;
        private readonly KeyPair _keyPair;
        private readonly ECPrivKey _cachedPrivKey;

        private SignatureProvider(KeyPair keyPair)
        {
            _keyPair = keyPair;
            _cachedPrivKey = ECPrivKey.Create(keyPair.PrivKey.Bytes);
        }

        /// <summary>
        /// Creates a new <see cref="SignatureProvider"/> with a generated key pair.
        /// </summary>
        /// <returns>The created <see cref="SignatureProvider"/>.</returns>
        public static SignatureProvider Create()
        {
            return new SignatureProvider(GenerateKeyPair());
        }

        /// <summary>
        /// Creates a new <see cref="SignatureProvider"/> out of the private key.
        /// </summary>
        /// <param name="privKey">Buffer containing the private key. Has to contain 32 bytes.</param>
        /// <returns>The created <see cref="SignatureProvider"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static SignatureProvider Create(Buffer privKey)
        {
            return new SignatureProvider(new KeyPair(privKey));
        }

        /// <summary>
        /// Creates a new <see cref="SignatureProvider"/> with the given <see cref="KeyPair"/>.
        /// </summary>
        /// <param name="keyPair">Key pair that is used to sign.</param>
        /// <returns>The created <see cref="SignatureProvider"/>.</returns>
        public static SignatureProvider Create(KeyPair keyPair)
        {
            return new SignatureProvider(keyPair);
        }

        /// <summary>
        /// Generates a new <see cref="KeyPair"/>.
        /// </summary>
        /// <returns>The generated key pair.</returns>
        public static KeyPair GenerateKeyPair()
        {
            return new KeyPair(KeyPair.GeneratePrivKey());
        }

        /// <summary>
        /// Ensures that a buffer is a valid message.
        /// </summary>
        /// <param name="message">The buffer to check for a message.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void EnsureValidMessage(Buffer message)
        {
            if (message.Length != 32)
                throw new ArgumentOutOfRangeException(nameof(message), "has to be 32 bytes");
        }

        /// <inheritdoc/>
        public Signature Sign(Buffer buffer)
        {
            EnsureValidMessage(buffer);

            var sig = _cachedPrivKey.SignECDSARFC6979(buffer.Bytes);
            var compact = new byte[64];
            sig.WriteCompactToSpan(compact);

            return new Signature(PubKey, Buffer.From(compact));
        }

        /// <summary>
        /// Verifies the signature is correct for the given <see cref="Buffer"/>.
        /// </summary>
        /// <param name="sig">The signature go verify.</param>
        /// <param name="buffer">The buffer the signature was created from.</param>
        /// <returns>True if the signature is valid, false if not.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool Verify(Signature sig, Buffer buffer)
        {
            EnsureValidMessage(buffer);

            if (!SecpECDSASignature.TryCreateFromCompact(sig.Hash.Bytes, out var signature))
                return false;

            var pubKey = ECPubKey.Create(sig.PubKey.Bytes);
            return pubKey.SigVerify(signature, buffer.Bytes);
        }

        /// <summary>
        /// Verifies the signature is correct for the given <see cref="Buffer"/>.
        /// </summary>
        /// <param name="pubKey">The pubkey to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="buffer">The buffer the signature was created from.</param>
        /// <returns>True if the signature is valid, false if not.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool Verify(Buffer pubKey, Buffer signature, Buffer buffer)
        {
            return Verify(new Signature(pubKey, signature), buffer);
        }
    }
}
