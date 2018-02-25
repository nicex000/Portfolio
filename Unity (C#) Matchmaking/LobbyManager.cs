using System.Collections;
using System.Collections.Generic;
using DG.DemiLib;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class LobbyManager : NetworkLobbyManager
{
    private NetworkLobbyManager lobbyManager;

    public GameObject matchmakingPanel;
    public GameObject connectionPanel;

    public GameObject MMHostPanel;
    public GameObject MMJoinPanel;

    public GameObject LobbyHostPanel;
    public GameObject LobbyClientPanel;

    public Text matchNameHost;
    private string mName;
    public Dropdown matchmakingServer;
    public Slider MaxPlayersSlider;
    public Toggle isVisible;
    public Text password;

    public Text matchNameJoin;
    public Text passwordJoin;


    public Text LastNotif;


    public bool isReady;

    //publicClientAddress
    //privateClientAddress
    int eloSkillLevel = 0;

    int requestDomain = 1;

    private string quickMatchName = "QuickMatch";

    private int lobbyPlayersConnected = 0;
    private int playersConnected = 0;


    public GameObject socialScreen;
    public GameObject preGameScreen;
    public GameObject readyButton;

    public GameObject pregamePlayerParent;
    public GameObject pregamePlayerPrefab;
    public Text pregameMatchNameText;
    private string lastClickedMatchName;

    public GameObject matchButtonParent;
    public GameObject matchButtonPrefab;

    public Text matchNameInputField;
    public Text matchNameInputPlaceholder;

    public GameObject blackScreen;

    public List<GameObject> matchButtonList;

    //private int lastLobbyPlayerAmount = 0;
    private bool[] lastLobbyPlayerStatus;
    private float refreshTime = 1;
    private float timeTillNextRefresh;

    // Use this for initialization
    private void Start()
    {
        lobbyManager = GetComponent<LobbyManager>();
        matchButtonList = new List<GameObject>();
        timeTillNextRefresh = refreshTime;
    }

    private void Update()
    {
        if (preGameScreen != null)
        {
            timeTillNextRefresh -= Time.deltaTime;

            if (timeTillNextRefresh <= 0)
            {
                if (preGameScreen.activeInHierarchy) // refresh pre game screen
                {
                    /*Debug.Log("Refreshing lobby players");
                    int newLobbyPlayerAmount = 0;

                    foreach (NetworkLobbyPlayer p in lobbySlots)
                    {
                        if (p != null)
                        {
                            newLobbyPlayerAmount++;
                        }
                    }

                    Debug.Log("Old players: " + lastLobbyPlayerAmount + ", new: " + newLobbyPlayerAmount);
                    if (newLobbyPlayerAmount != lastLobbyPlayerAmount)
                    {*/
                    for (int i = 0; i < pregamePlayerParent.transform.childCount; i++)
                    {
                        Destroy(pregamePlayerParent.transform.GetChild(i).gameObject);
                    }

                    bool allPlayersReady = true;
                    bool entered = false;
                    for (int slotIndex = 0; slotIndex < lobbySlots.Length; slotIndex++)
                    {
                        NetworkLobbyPlayer p = lobbySlots[slotIndex];
                        if (slotIndex == 0 && p == null)
                        {
                            allPlayersReady = false;
                        }
                        if (p != null)
                        {
                            GameObject pregamePlayer = Instantiate(pregamePlayerPrefab, Vector3.zero/*pregamePlayerParent.transform.position*/, Quaternion.identity, pregamePlayerParent.transform);
                            RectTransform rt = pregamePlayer.GetComponent<RectTransform>();
                            rt.position.Set(0, 0/*-80 - slotIndex * 80*/, 0);
                            rt.offsetMin = new Vector2(15, 0);
                            rt.offsetMax = new Vector2(-15, 60);
                            rt.localPosition= rt.localPosition.WithY(105 + slotIndex * -60);
                            pregamePlayer.transform.GetChild(0).GetComponent<Text>().text = "Player " + slotIndex + " (" + p.readyToBegin + ")";//PlayerPrefs.GetString("");
                            if (!p.readyToBegin)
                            {
                                allPlayersReady = false;
                            }
                        }
                        entered = true;
                    }
                    if (allPlayersReady && entered)
                    {
                        LobbyPlayer.waitingScreenStruct s;
                        s.canvas = socialScreen.transform.root.gameObject;
                        s.loadingScreen = blackScreen;
                        FindObjectOfType<LobbyPlayer>().RpcLoadingScreen(s);
                        socialScreen.transform.root.gameObject.SetActive(false);
                        blackScreen.SetActive(true);
                    }
                    //}

                    //lastLobbyPlayerAmount = newLobbyPlayerAmount;
                }
                else //Request matchlist
                {
                    lobbyManager.matchMaker.ListMatches(0, 10, "", false, eloSkillLevel, requestDomain, OnReceiveMatchList);
                }

                timeTillNextRefresh = refreshTime;
            }
        }
    }

    //MATCHMAKING
    public void OnSocialButtonClicked()
    {
        Debug.Log("Requesting match list.");
        //if(!lobbyManager.isNetworkActive)
        lobbyManager.StartMatchMaker();
        lobbyManager.matchMaker.ListMatches(0, 10, "", false, eloSkillLevel, requestDomain, OnReceiveMatchList);
    }

    public void StopMatchMaking()
    {
        lobbyManager.StopMatchMaker();
    }

    public void OnMatchCreationMenu()
    {
        mName = GenerateRandomMatchName();
        matchNameInputPlaceholder.text = mName;
    }

    private string GenerateRandomMatchName()
    {
        string name = "";

        for(int i = 0; i < 4; i++)
        {
            name += Random.Range(0, 9);//(char) ('a' + Random.Range(0, 26));
        }
        
        return name;
    }

    public void OnReceiveMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        if (!success)
            return;

        if (matchButtonList.Count > 0)
        {
            foreach (GameObject button in matchButtonList)
            {
                Destroy(button);
            }
        }

        int i = 0;
        foreach (MatchInfoSnapshot matchInfo in matches)
        {
            if (matchInfo.isPrivate)
                continue;
            
            GameObject hostButton = Instantiate(matchButtonPrefab, matchButtonParent.transform);
            hostButton.transform.GetChild(0).GetComponent<Text>().text = matchInfo.name;
            RectTransform rt = hostButton.GetComponent<RectTransform>();
            //rt.SetPositionY(rt.position.y - (50 * i));
            rt.offsetMin = new Vector2(-80, 0);
            rt.offsetMax = new Vector2(80, 60);
            rt.localPosition = rt.localPosition.WithY(105 + i * -70);
            hostButton.GetComponent<Button>().onClick.AddListener(() => OnJoinMatchClicked(matchInfo.networkId, matchInfo.name));
            matchButtonList.Add(hostButton);

            i++;
        }
    }

    public void OnJoinMatchClicked(NetworkID match, string matchName)
    {
        Debug.Log("Attempting to connect to match " + matchName + " (id: " + match + ")." );
        socialScreen.SetActive(false);
        lobbyManager.matchMaker.JoinMatch(match, "", "", "", eloSkillLevel, requestDomain, OnMatchJoined);
        preGameScreen.SetActive(true);
        lastClickedMatchName = matchName;
    }

    public void OnCreateMatchButtonClicked()
    {
        if (matchNameInputField.text != "")
            mName = matchNameInputField.text;

        lobbyManager.matchMaker.CreateMatch(mName, 4, true, "", "", "", eloSkillLevel, requestDomain, OnMatchCreate);
    }

    public void OnReadyButtonClicked()
    {
        isReady = true;
        readyButton.SetActive(false);
    }


    //////////////////////////////////////////////
    public void OnMatchMakingButtonClicked()
    {
        connectionPanel.SetActive(false);
        MMHostPanel.SetActive(false);
        MMJoinPanel.SetActive(false);
        LobbyHostPanel.SetActive(false);
        LobbyClientPanel.SetActive(false);
        matchmakingPanel.SetActive(true);
        lobbyManager.StartMatchMaker();
    }

    public void OnMMHostMenu()
    {
        setServer();
        matchmakingPanel.SetActive(false);
        MMHostPanel.SetActive(true);
    }

    public void OnMMHost()
    {
        mName = matchNameHost.text;
        lobbyManager.matchMaker.ListMatches(0, 10, mName, false, eloSkillLevel, requestDomain, OnMatchHostList);
    }

    public void OnMMFindMenu()
    {
        setServer();
        matchmakingPanel.SetActive(false);
        MMJoinPanel.SetActive(true);
    }

    public void OnMMConnect()
    {
        mName = matchNameJoin.text;
        lobbyManager.matchMaker.ListMatches(0, 10, "", false, eloSkillLevel, requestDomain, OnMatchList);
    }

    public void OnMMBack()
    {
        connectionPanel.SetActive(true);
        matchmakingPanel.SetActive(false);
        lobbyManager.StopMatchMaker();
    }

    public void OnReadyHost()
    {
        //lobbyManager.StartHost();
        isReady = true;
    }

    public void OnReadyClient()
    {
        //lobbyManager.StartClient();
        isReady = true;
    }

    void setServer()
    {
        switch (matchmakingServer.value)
        {
            case 0:
                lobbyManager.matchHost = "mm.unet.unity3d.com";
                break;
            case 1:
                lobbyManager.matchHost = "172.0.0.1";
                break;

            case 2:
                lobbyManager.matchHost = "us1-mm.unet.unity3d.com";
                break;

            case 3:
                lobbyManager.matchHost = "eu1-mm.unet.unity3d.com";
                break;

            case 4:
                lobbyManager.matchHost = "ap1-mm.unet.unity3d.com";
                break;
        }
    }

    public void OnMatchHostList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        if (matches.Count > 0)
        {
            mName = mName + "(" + matches.Count + ")";
            lobbyManager.matchMaker.ListMatches(0, 10, mName, false, eloSkillLevel, requestDomain, OnMatchHostList);
        }
        else
        {
            lobbyManager.matchMaker.CreateMatch(matchNameHost.text, (uint) MaxPlayersSlider.value, isVisible.isOn,
                password.text, "", "", eloSkillLevel, requestDomain, OnMatchCreate);
        }
    }

    public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        base.OnMatchCreate(success, extendedInfo, matchInfo);
        //MMHostPanel.SetActive(false);
        //LobbyHostPanel.SetActive(true);
        if (success)
        {
            Debug.Log("Create match succeeded");
            //LastNotif.text = "Create match succeeded";

            //LobbyHostPanel.transform.GetChild(2).GetComponent<Text>().text = "Match #" + matchInfo.networkId;
            //LobbyHostPanel.transform.GetChild(3).GetComponent<Text>().text = mName;
            Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);
            NetworkServer.Listen(matchInfo, 9000);

            //lobbyManager.StartHost(matchInfo);
        }
        else
        {
            Debug.LogWarning("Create match failed, retrying...");
            //LastNotif.text = "Create match failed, retrying...";
            lobbyManager.matchMaker.CreateMatch(matchNameHost.text, (uint) MaxPlayersSlider.value, isVisible.isOn,
                password.text, "", "", eloSkillLevel, requestDomain, OnMatchCreate);
        }
    }

    public void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        Debug.Log(matches.Count);
        int index = 0;
        index = matches.FindIndex(isMatch);
        if (index == -1 || matches[index].currentSize < 1)
        {
            Debug.LogWarning("unable to find a match with that name");
            LastNotif.text = "unable to find a match with that name";
        }
        else
        {
            Debug.Log("playerSize" + matches[index].currentSize);
            lobbyManager.matchMaker.JoinMatch(matches[index].networkId, passwordJoin.text, "", "", eloSkillLevel,
                requestDomain, OnMatchJoined);
        }
    }

    public override void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        base.OnMatchJoined(success, extendedInfo, matchInfo);
        /*MMJoinPanel.SetActive(false);
        LobbyClientPanel.SetActive(true);*/
        if (success)
        {
            /*Debug.Log("Joining the match");
            LastNotif.text = "Joining the match";
            LobbyClientPanel.transform.GetChild(2).GetComponent<Text>().text = "Match #" + matchInfo.networkId;
            LobbyClientPanel.transform.GetChild(3).GetComponent<Text>().text = mName;*/
            socialScreen.SetActive(false);
            preGameScreen.SetActive(true);
            Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);
            /* NetworkClient nc = lobbyManager.StartClient(matchInfo);
             print(nc.connection.isConnected);*/
            //lobbyManager.StartClient(matchInfo);

            /*  MatchInfo hostInfo = matchInfo;
              NetworkManager.singleton.StartClient(hostInfo);*/
        }
        else
        {
            Debug.LogWarning("Join match failed");
            //LastNotif.text = "Join match failed, retrying...";
            lobbyManager.matchMaker.JoinMatch(matchInfo.networkId, passwordJoin.text, "", "", eloSkillLevel,
                requestDomain, OnMatchJoined);
        }
    }

    public void DisbandLobby()
    {
        lobbyManager.matchMaker.DestroyMatch(lobbyManager.matchInfo.networkId, requestDomain, OnDestroyMatch);

    }

    public override void OnDestroyMatch(bool success, string extendedInfo)
    {
        if (success)
        {
            base.OnDestroyMatch(success, extendedInfo);
            NetworkServer.DisconnectAll();
            OnMatchMakingButtonClicked();
            Debug.Log("match destroyed");
        }
        else
        {
            Debug.Log("unable to destroy lobby");
        }
    }

    public void LeaveLobby()
    {
        Network.Disconnect();
        OnMatchMakingButtonClicked();
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        Debug.LogError(conn.lastError);
    }



    public override void OnLobbyClientConnect(NetworkConnection conn)
    {
        base.OnLobbyClientConnect(conn);
        Debug.Log("client is CONNECTED");
        //LastNotif.text = "Connected";
    }

    bool isMatch(MatchInfoSnapshot m)
    {
        return m.name == mName;
    }

    



//QUICKMATCH

public void StartQuickMatch()
    {
        //connectionPanel.SetActive(false);
        //TODO: quickmatch UI
        lobbyManager.StartMatchMaker();
        lobbyManager.matchMaker.ListMatches(0, 10, "", true, eloSkillLevel, requestDomain, OnQuickMatchFindGames);
    }

    public void OnQuickMatchFindGames(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        if (matches.Count > 0)
        {
            Debug.Log(matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                if (!(matches[i].currentSize >= matches[i].maxSize || matches[i].isPrivate) &&
                    matches[i].currentSize > 0)
                {
                    if (TryJoin(matches[i]))
                    {
                        i = matches.Count;
                    }
                }
            }
        }
        else
        {
            lobbyManager.matchMaker.CreateMatch(quickMatchName, 4, true/*isVisible.isOn*/, "", "", "", eloSkillLevel,
                requestDomain, OnQuickMatchCreate);
        }
    }

    public bool TryJoin(MatchInfoSnapshot match)
    {
        if (match.currentSize > 0)
        {
            Debug.Log("playerSize: " + match.currentSize);

            lobbyManager.matchMaker.JoinMatch(match.networkId, "", "", "", eloSkillLevel, requestDomain,
                OnQuickMatchJoined);
            if (lobbyManager.matchName.Length > 0)
            {
                return true;
            }
        }
        return false;
    }

    public void OnQuickMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        base.OnMatchJoined(success, extendedInfo, matchInfo);

        if (success)
        {
            Debug.Log("Joining the match");

            Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);

        }
        else
        {
            Debug.LogWarning("Join match failed");
        }
    }




    public void OnQuickMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        base.OnMatchCreate(success, extendedInfo, matchInfo);
        if (success)
        {
            Debug.Log("Create quick match succeeded");
            //LastNotif.text = "Create quick match succeeded";

            LobbyHostPanel.transform.GetChild(2).GetComponent<Text>().text = "Match #" + matchInfo.networkId;
            LobbyHostPanel.transform.GetChild(3).GetComponent<Text>().text = mName;
            Utility.SetAccessTokenForNetwork(matchInfo.networkId, matchInfo.accessToken);
            NetworkServer.Listen(matchInfo, 9000);

        }
        else
        {
            Debug.LogWarning("Create quick match failed, retrying...");
            //LastNotif.text = "Create quick match failed, retrying...";
            lobbyManager.matchMaker.CreateMatch(quickMatchName, 4, isVisible.isOn, "", "", "", eloSkillLevel,
                requestDomain, OnQuickMatchCreate);
        }
    }

    private bool lobbyServerPlayersReadyHasBeenCalled = false;
    public override void OnLobbyServerPlayersReady()
    {
        LobbyPlayer.waitingScreenStruct s;
        s.canvas = socialScreen.transform.root.gameObject;
        s.loadingScreen = blackScreen;
        FindObjectOfType<LobbyPlayer>().RpcLoadingScreen(s);
        if (lobbyServerPlayersReadyHasBeenCalled)
            return;
        else
            lobbyServerPlayersReadyHasBeenCalled = true;

        base.OnLobbyServerPlayersReady();
        Debug.Log("OnLobbyServerPlayersReady");
        foreach (var lobbyPlayer in lobbySlots)
        {
            if (lobbyPlayer != null)
            {
                lobbyPlayersConnected++;
                //GameObject pregamePlayer = Instantiate(pregamePlayerPrefab);
                //pregamePlayer.transform.SetParent(pregamePlayerParent.transform);
                //pregamePlayer.transform.GetChild(0).GetComponent<Text>().text = "Player " + lobbyPlayersConnected;//PlayerPrefs.GetString("");
            }
        }
        Debug.Log("lobby player: " + lobbyPlayersConnected);
    }


    public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
    {
        if (gamePlayer == null)
        {
            gamePlayer = Instantiate(gamePlayerPrefab);
        }

        playersConnected++;
        Debug.Log("game: " + playersConnected);

        if (lobbyPlayersConnected > 1 && playersConnected >= lobbyPlayersConnected && FindObjectOfType<MoveTween>().isThisServer())
        {
            //startPlaying = true;
            Debug.Log("rdy");
            FindObjectOfType<MoveTween>().startPlaying = true;
        }

        return base.OnLobbyServerSceneLoadedForPlayer(lobbyPlayer, gamePlayer);
    }

    //only for solo game (OnLobbyServerSceneLoadedForPlayer acts too fast)
    public override void OnLobbyServerSceneChanged(string sceneName)
    {
        base.OnLobbyServerSceneChanged(sceneName);
        if (lobbyPlayersConnected == 1)
        {
            //startPlaying = true;
            Debug.Log("rdy");
            FindObjectOfType<MoveTween>().startPlaying = true;
        }
    }
}
