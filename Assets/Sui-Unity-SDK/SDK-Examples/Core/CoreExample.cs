using System.Collections;
using System.Collections.Generic;
using OpenDive.BCS;
using Sui.Accounts;
using UnityEngine;
using static Sui.Cryptography.SignatureUtils;

public class CoreExample : MonoBehaviour
{
    [Header("Gen Account with ED25519 signature scheme")]
    [SerializeField]
    [Tooltip("Private key in hex, generated when explicitly declaring the signature scheme.")]
    private string privateKeyExplicit;

    [SerializeField]
    [Tooltip("Private key in bytes, generated when explicitly declaring the signature scheme.")]
    private string privateKeyExplicitBytes;

    [SerializeField]
    [Tooltip("Sui address")]
    private string suiAddressFromPrivateKeyExplicit;

    [Header("Gen Account with implicit ED25519 signature scheme")]
    [SerializeField]
    [Tooltip("Private key generated when creating an account without signature scheme.")]
    private string privateKeyImplicit;

    [SerializeField]
    [Tooltip("Private key in bytes, generated when creating an account without signature scheme.")]
    private string privateKeyImplicitBytes;

    [SerializeField]
    [Tooltip("Sui address")]
    private string suiAddressFromPrivateKeyImplicit;

    [Header("Gen account with secret key")]
    [SerializeField]
    [Tooltip("Secret key used to generate an account from it.")]
    public string secreteKey = "";

    [SerializeField]
    [Tooltip("Public key from account derived with secret key above.")]
    private string publicKeyFromAccountDerived;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateEd25519KeyPair()
    {

    }

    /// <summary>
    /// Create an ED25519 Account
    /// </summary>
    public void GenEd25519Account()
    {
        // You can create an ED25519 Account by explicitly defining the signature scheme
        Account accountEd25519Explicit = new Account(SignatureScheme.Ed25519);
        privateKeyExplicit = accountEd25519Explicit.PrivateKey.KeyHex;
        privateKeyExplicitBytes = string.Join(",", accountEd25519Explicit.PrivateKey.KeyBytes);
        suiAddressFromPrivateKeyExplicit = accountEd25519Explicit.SuiAddress().ToString();

        // Creating an Account without explicitly defining the signature scheme
        //      generates an ED25519 Account
        Account accountEd25519Implicit = new Account();
        privateKeyImplicit = accountEd25519Implicit.PrivateKey.KeyHex;
        privateKeyImplicitBytes = string.Join(",", accountEd25519Implicit.PrivateKey.KeyBytes);
        suiAddressFromPrivateKeyImplicit = accountEd25519Implicit.SuiAddress().ToString();
    }

    /// <summary>
    /// Create an ED25519 Account from secret key
    /// </summary>
    public void GenEd25519AccountFromSecretKey()
    {
        Account accountFromSecreteKey = new Account(secreteKey);
        publicKeyFromAccountDerived = accountFromSecreteKey.PublicKey.KeyHex;
    }
}
