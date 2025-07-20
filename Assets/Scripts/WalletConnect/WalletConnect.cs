using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thirdweb.Unity.Examples
{
    [System.Serializable]
    public class WalletPanelUI
    {
        public string Identifier;
        public GameObject Panel;
        public Button Action1Button;
        public Button Action2Button;
        public Button Action3Button;
        public Button BackButton;
        public Button NextButton;
        public TMP_Text LogText;
        public TMP_InputField InputField;
        public Button InputFieldSubmitButton;
    }

    public class WalletConnect : MonoBehaviour
    {
        public static WalletConnect Instance { get; private set; }


        [field: SerializeField, Header("Wallet Options")]
        private ulong ActiveChainId = 421614;

        [field: SerializeField]
        private bool WebglForceMetamaskExtension = false;

        [field: SerializeField, Header("Connect Wallet")]
        private GameObject ConnectWalletPanel;

        [field: SerializeField]
        private TextMeshProUGUI WalletAdrress;
        private string _walletAddress = string.Empty;
        [field: SerializeField]
        private TextMeshProUGUI WalletBalance;


        [field: SerializeField]
        private Button EcosystemWalletButton;

        [field: SerializeField]
        private Button WalletConnectButton;

        [field: SerializeField, Header("Wallet Panels")]
        private List<WalletPanelUI> WalletPanels;


        private ThirdwebChainData _chainDetails;

        [field: SerializeField]
        private Button playGameButton;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);


            InitializePanels();
            playGameButton.interactable = false;
        }

        private async void Start()
        {
            try
            {
                _chainDetails = await Utils.GetChainMetadata(client: ThirdwebManager.Instance.Client, chainId: ActiveChainId);
            }
            catch
            {
                _chainDetails = new ThirdwebChainData()
                {
                    NativeCurrency = new ThirdwebChainNativeCurrency()
                    {
                        Decimals = 18,
                        Name = "DEVIL",
                        Symbol = "DEVIL"
                    }
                };
            }
        }

        private void InitializePanels()
        {
            CloseAllPanels();

            ConnectWalletPanel.SetActive(true);

            EcosystemWalletButton.onClick.RemoveAllListeners();
            EcosystemWalletButton.onClick.AddListener(() => InitializeEcosystemWalletPanel_Socials());

            WalletConnectButton.onClick.RemoveAllListeners();
            WalletConnectButton.onClick.AddListener(() =>
            {
                var options = GetWalletOptions(WalletProvider.WalletConnectWallet);
                ConnectWallet(options);
            });
        }

        private async void ConnectWallet(WalletOptions options)
        {
            // Connect the wallet

            var internalWalletProvider = options.Provider == WalletProvider.MetaMaskWallet ? WalletProvider.WalletConnectWallet : options.Provider;
            var currentPanel = WalletPanels.Find(panel => panel.Identifier == internalWalletProvider.ToString());

            //Log(currentPanel.LogText, $"Connecting...");

            var wallet = await ThirdwebManager.Instance.ConnectWallet(options);
            _walletAddress = await wallet.GetAddress();
            // Initialize the wallet panel

            CloseAllPanels();

            // Setup actions

            //ClearLog(currentPanel.LogText);
            //currentPanel.Panel.SetActive(true);
            string shortAddress = $"{_walletAddress.Substring(0, 6)}...{_walletAddress.Substring(_walletAddress.Length - 6)}";
            WalletAdrress.text = $"Wallet: {shortAddress}";

            var balance = await wallet.GetBalance(chainId: ActiveChainId);
            var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 2, addCommas: true);

            WalletBalance.text = $"Balance: {balanceEth.ToString()} {_chainDetails.NativeCurrency.Symbol}";

            playGameButton.interactable = true;

            // Hide the wallet panel UI
            //currentPanel.Panel.SetActive(false);
            ConnectWalletPanel.SetActive(false);

            //currentPanel.BackButton.onClick.RemoveAllListeners();
            //currentPanel.BackButton.onClick.AddListener(InitializePanels);

            //currentPanel.NextButton.onClick.RemoveAllListeners();
            //currentPanel.NextButton.onClick.AddListener(InitializeContractsPanel);

            //currentPanel.Action1Button.onClick.RemoveAllListeners();
            //currentPanel.Action1Button.onClick.AddListener(async () =>
            //{
            //    var address = await wallet.GetAddress();
            //    address.CopyToClipboard();
            //    Log(currentPanel.LogText, $"Address: {address}");
            //});

            //currentPanel.Action2Button.onClick.RemoveAllListeners();
            //currentPanel.Action2Button.onClick.AddListener(async () =>
            //{
            //    var message = "Hello World!";
            //    var signature = await wallet.PersonalSign(message);
            //    Log(currentPanel.LogText, $"Signature: {signature}");
            //});

            //currentPanel.Action3Button.onClick.RemoveAllListeners();
            //currentPanel.Action3Button.onClick.AddListener(async () =>
            //{
            //    LoadingLog(currentPanel.LogText);
            //    var balance = await wallet.GetBalance(chainId: ActiveChainId);
            //    var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 4, addCommas: true);
            //    Log(currentPanel.LogText, $"Balance: {balance} {_chainDetails.NativeCurrency.Symbol}");
            //});
        }

        private WalletOptions GetWalletOptions(WalletProvider provider)
        {
            switch (provider)
            {
                case WalletProvider.EcosystemWallet:
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Google);
                    return new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                case WalletProvider.WalletConnectWallet:
                    var externalWalletProvider =
                        Application.platform == RuntimePlatform.WebGLPlayer && WebglForceMetamaskExtension ? WalletProvider.MetaMaskWallet : WalletProvider.WalletConnectWallet;
                    return new WalletOptions(provider: externalWalletProvider, chainId: ActiveChainId);
                default:
                    throw new System.NotImplementedException("Wallet provider not implemented for this example.");
            }
        }


        private void InitializeEcosystemWalletPanel_Socials()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "EcosystemWallet_Socials");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializePanels);

            // socials action 1 is google, 2 is apple 3 is discord

            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(() =>
            {
                try
                {
                    Log(panel.LogText, "Authenticating...");
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Google);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            panel.Action2Button.onClick.RemoveAllListeners();
            panel.Action2Button.onClick.AddListener(() =>
            {
                try
                {
                    Log(panel.LogText, "Authenticating...");
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Apple);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(() =>
            {
                try
                {
                    Log(panel.LogText, "Authenticating...");
                    var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.the-bonfire", authprovider: AuthProvider.Discord);
                    var options = new WalletOptions(provider: WalletProvider.EcosystemWallet, chainId: ActiveChainId, ecosystemWalletOptions: ecosystemWalletOptions);
                    ConnectWallet(options);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private void InitializeContractsPanel()
        {
            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "Contracts");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializePanels);

            panel.NextButton.onClick.RemoveAllListeners();
            panel.NextButton.onClick.AddListener(InitializeAccountAbstractionPanel);

            // Get NFT
            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var dropErc1155Contract = await ThirdwebManager.Instance.GetContract(address: "0x94894F65d93eb124839C667Fc04F97723e5C4544", chainId: ActiveChainId);
                    var nft = await dropErc1155Contract.ERC1155_GetNFT(tokenId: 1);
                    Log(panel.LogText, $"NFT: {JsonConvert.SerializeObject(nft.Metadata)}");
                    var sprite = await nft.GetNFTSprite(client: ThirdwebManager.Instance.Client);
                    // spawn image for 3s
                    var image = new GameObject("NFT Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    image.transform.SetParent(panel.Panel.transform, false);
                    image.GetComponent<Image>().sprite = sprite;
                    Destroy(image, 3f);
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            // Call contract
            panel.Action2Button.onClick.RemoveAllListeners();
            //panel.Action2Button.onClick.AddListener(async () =>
            //{
            //    try
            //    {
            //        LoadingLog(panel.LogText);
            //        var contract = await ThirdwebManager.Instance.GetContract(address: "0x0dA20F06cD5a92965a3F9Bed50400b4561dE4B85", chainId: ActiveChainId);

            //        var result = await ThirdwebContract.Read<string>(contract, "setCustomGreeting");

            //        Log(panel.LogText, $"Result (uri): {result}");
            //    }
            //    catch (System.Exception e)
            //    {
            //        Log(panel.LogText, e.Message);
            //    }
            //});
            panel.Action2Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);

                    // Load ABI from Resources folder
                    TextAsset abiAsset = Resources.Load<TextAsset>("GameReward");
                    string abiJson = abiAsset.text;


                    Debug.Log($"ABI JSON: {abiJson}");

                    // Create contract manually with ABI
                    var contract = await ThirdwebManager.Instance.GetContract(
                        address: "0x1Fb27da20943A11f14840d553684f352FADFBef2",
                        chainId: ActiveChainId,
                        abi: abiJson);


                    var result = await Thirdweb.ThirdwebContract.Read<string>(
                        contract,
                        "characterNFT" // or your actual method name
                    );

                    Log(panel.LogText, $"Result: {result}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });


            // Get ERC20 Balance
            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var dropErc20Contract = await ThirdwebManager.Instance.GetContract(address: "0xEBB8a39D865465F289fa349A67B3391d8f910da9", chainId: ActiveChainId);
                    var symbol = await dropErc20Contract.ERC20_Symbol();
                    var balance = await dropErc20Contract.ERC20_BalanceOf(ownerAddress: await ThirdwebManager.Instance.GetActiveWallet().GetAddress());
                    var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 0, addCommas: false);
                    Log(panel.LogText, $"Balance: {balanceEth} {symbol}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private async void InitializeAccountAbstractionPanel()
        {
            var currentWallet = ThirdwebManager.Instance.GetActiveWallet();
            var smartWallet = await ThirdwebManager.Instance.UpgradeToSmartWallet(personalWallet: currentWallet, chainId: ActiveChainId, smartWalletOptions: new SmartWalletOptions(sponsorGas: true));

            var panel = WalletPanels.Find(walletPanel => walletPanel.Identifier == "AccountAbstraction");

            CloseAllPanels();

            ClearLog(panel.LogText);
            panel.Panel.SetActive(true);

            panel.BackButton.onClick.RemoveAllListeners();
            panel.BackButton.onClick.AddListener(InitializePanels);

            // Personal Sign (1271)
            panel.Action1Button.onClick.RemoveAllListeners();
            panel.Action1Button.onClick.AddListener(async () =>
            {
                try
                {
                    var message = "Hello, World!";
                    var signature = await smartWallet.PersonalSign(message);
                    Log(panel.LogText, $"Signature: {signature}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            // Create Session Key
            panel.Action2Button.onClick.RemoveAllListeners();
            panel.Action2Button.onClick.AddListener(async () =>
            {
                try
                {
                    Log(panel.LogText, "Granting Session Key...");
                    var randomWallet = await PrivateKeyWallet.Generate(ThirdwebManager.Instance.Client);
                    var randomWalletAddress = await randomWallet.GetAddress();
                    var timeTomorrow = Utils.GetUnixTimeStampNow() + 60 * 60 * 24;
                    var sessionKey = await smartWallet.CreateSessionKey(
                        signerAddress: randomWalletAddress,
                        approvedTargets: new List<string> { Constants.ADDRESS_ZERO },
                        nativeTokenLimitPerTransactionInWei: "0",
                        permissionStartTimestamp: "0",
                        permissionEndTimestamp: timeTomorrow.ToString(),
                        reqValidityStartTimestamp: "0",
                        reqValidityEndTimestamp: timeTomorrow.ToString()
                    );
                    Log(panel.LogText, $"Session Key Created for {randomWalletAddress}: {sessionKey.TransactionHash}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });

            // Get Active Signers
            panel.Action3Button.onClick.RemoveAllListeners();
            panel.Action3Button.onClick.AddListener(async () =>
            {
                try
                {
                    LoadingLog(panel.LogText);
                    var activeSigners = await smartWallet.GetAllActiveSigners();
                    Log(panel.LogText, $"Active Signers: {JsonConvert.SerializeObject(activeSigners)}");
                }
                catch (System.Exception e)
                {
                    Log(panel.LogText, e.Message);
                }
            });
        }

        private void CloseAllPanels()
        {
            ConnectWalletPanel.SetActive(false);
            foreach (var walletPanel in WalletPanels)
            {
                walletPanel.Panel.SetActive(false);
            }
        }

        private void ClearLog(TMP_Text logText)
        {
            logText.text = string.Empty;
        }

        private void Log(TMP_Text logText, string message)
        {
            logText.text = message;
            ThirdwebDebug.Log(message);
        }

        private void LoadingLog(TMP_Text logText)
        {
            logText.text = "Loading...";
        }
    }
}
