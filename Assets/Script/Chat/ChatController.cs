using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Chat.Demo;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Chat
{
    public class ChatController : MonoBehaviour, IChatClientListener
    {
        [SerializeField] private string[] channelsToJoinOnConnect; // set in inspector. Demo channels to join automatically.

        [SerializeField] private string[] friendsList;

        private int historyLengthToFetch; // set in inspector. Up to a certain degree, previously sent messages can be fetched for context

        public string UserName { get; set; } = "Yumemi";

        private string selectedChannelName; // mainly used for GUI/input

        private ChatClient chatClient;

#if !PHOTON_UNITY_NETWORKING
        public ChatAppSettings ChatAppSettings
        {
            get { return this.chatAppSettings; }
        }

        [SerializeField]
#endif
        protected internal ChatAppSettings chatAppSettings;

        [SerializeField] private GameObject missingAppIdErrorPanel;
        [SerializeField] private GameObject connectingLabel;

        [SerializeField] private GameObject chatPanel;     // set in inspector (to enable/disable panel)
        [SerializeField] private InputField InputFieldChat;   // set in inspector
        [SerializeField] private Text currentChannelText;     // set in inspector
        [SerializeField] private Toggle channelToggleToInstantiate; // set in inspector


        [SerializeField] private FriendItem friendListUiItemtoInstantiate;

        private readonly Dictionary<string, Toggle> channelToggles = new Dictionary<string, Toggle>();

        private readonly Dictionary<string, FriendItem> friendListItemLUT = new Dictionary<string, FriendItem>();

        [SerializeField] private bool showState = true;
        [SerializeField] private Text stateText; // set in inspector
        [SerializeField] private Text userIdText; // set in inspector
        [SerializeField] private GameObject startObj; 

        public void Start()
        {
            this.userIdText.text = "";
            this.stateText.text = "";
            this.stateText.gameObject.SetActive(true);
            this.userIdText.gameObject.SetActive(true);
            this.chatPanel.gameObject.SetActive(false);
            this.connectingLabel.SetActive(false);

            if (string.IsNullOrEmpty(this.UserName))
            {
                this.UserName = "user" + Environment.TickCount % 99; //made-up username
            }

#if PHOTON_UNITY_NETWORKING
            this.chatAppSettings = PhotonNetwork.PhotonServerSettings.AppSettings.GetChatSettings();
#endif

            bool appIdPresent = !string.IsNullOrEmpty(this.chatAppSettings.AppIdChat);

            this.missingAppIdErrorPanel.SetActive(!appIdPresent);
            //this.userIdFormPanel.gameObject.SetActive(appIdPresent);

            if (!appIdPresent)
            {
                Debug.LogError("You need to set the chat app ID in the PhotonServerSettings file in order to continue.");
            }
        }

        public void Connect()
        {
            // this.userIdFormPanel.gameObject.SetActive(false);

            this.chatClient = new ChatClient(this);
#if !UNITY_WEBGL
            this.chatClient.UseBackgroundWorkerForSending = true;
#endif
            this.chatClient.AuthValues = new Photon.Chat.AuthenticationValues(this.UserName);
            this.chatClient.ConnectUsingSettings(this.chatAppSettings);
            startObj.SetActive(false);

            this.channelToggleToInstantiate.gameObject.SetActive(false);
            Debug.Log("Connecting as: " + this.UserName);

            this.connectingLabel.SetActive(true);
        }

        /// <summary>To avoid that the Editor becomes unresponsive, disconnect all Photon connections in OnDestroy.</summary>
        public void OnDestroy()
        {
            if (this.chatClient != null)
            {
                this.chatClient.Disconnect();
            }
        }

        /// <summary>To avoid that the Editor becomes unresponsive, disconnect all Photon connections in OnApplicationQuit.</summary>
        public void OnApplicationQuit()
        {
            if (this.chatClient != null)
            {
                this.chatClient.Disconnect();
            }
        }

        public void Update()
        {
            if (this.chatClient != null)
            {
                this.chatClient.Service(); // make sure to call this regularly! it limits effort internally, so calling often is ok!
            }

            // check if we are missing context, which means we got kicked out to get back to the Photon Demo hub.
            if (this.stateText == null)
            {
                Destroy(this.gameObject);
                return;
            }

            this.stateText.gameObject.SetActive(this.showState); // this could be handled more elegantly, but for the demo it's ok.
        }


        public void OnEnterSend()
        {
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                this.SendChatMessage(this.InputFieldChat.text);
                this.InputFieldChat.text = "";
            }
        }


        public int TestLength = 2048;
        private byte[] testBytes = new byte[2048];

        private void SendChatMessage(string inputLine)
        {
            if (string.IsNullOrEmpty(inputLine))
            {
                return;
            }
            if ("test".Equals(inputLine))
            {
                if (this.TestLength != this.testBytes.Length)
                {
                    this.testBytes = new byte[this.TestLength];
                }

                this.chatClient.SendPrivateMessage(this.chatClient.AuthValues.UserId, this.testBytes, true);
            }


            bool doingPrivateChat = this.chatClient.PrivateChannels.ContainsKey(this.selectedChannelName);
            string privateChatTarget = string.Empty;
            if (doingPrivateChat)
            {
                // the channel name for a private conversation is (on the client!!) always composed of both user's IDs: "this:remote"
                // so the remote ID is simple to figure out

                string[] splitNames = this.selectedChannelName.Split(new char[] { ':' });
                privateChatTarget = splitNames[1];
            }


            if (inputLine[0].Equals('\\'))
            {
                string[] tokens = inputLine.Split(new char[] { ' ' }, 2);
                //if (tokens[0].Equals("\\help"))
                //{
                //    this.PostHelpToCurrentChannel();
                //}
                if (tokens[0].Equals("\\state"))
                {
                    int newState = 0;


                    List<string> messages = new List<string>();
                    messages.Add("i am state " + newState);
                    string[] subtokens = tokens[1].Split(new char[] { ' ', ',' });

                    if (subtokens.Length > 0)
                    {
                        newState = int.Parse(subtokens[0]);
                    }

                    if (subtokens.Length > 1)
                    {
                        messages.Add(subtokens[1]);
                    }

                    this.chatClient.SetOnlineStatus(newState, messages.ToArray()); // this is how you set your own state and (any) message
                }
                else if ((tokens[0].Equals("\\subscribe") || tokens[0].Equals("\\s")) && !string.IsNullOrEmpty(tokens[1]))
                {
                    this.chatClient.Subscribe(tokens[1].Split(new char[] { ' ', ',' }));
                }
                else if ((tokens[0].Equals("\\unsubscribe") || tokens[0].Equals("\\u")) && !string.IsNullOrEmpty(tokens[1]))
                {
                    this.chatClient.Unsubscribe(tokens[1].Split(new char[] { ' ', ',' }));
                }
                else if (tokens[0].Equals("\\clear"))
                {
                    if (doingPrivateChat)
                    {
                        this.chatClient.PrivateChannels.Remove(this.selectedChannelName);
                    }
                    else
                    {
                        ChatChannel channel;
                        if (this.chatClient.TryGetChannel(this.selectedChannelName, doingPrivateChat, out channel))
                        {
                            channel.ClearMessages();
                        }
                    }
                }
                else if (tokens[0].Equals("\\msg") && !string.IsNullOrEmpty(tokens[1]))
                {
                    string[] subtokens = tokens[1].Split(new char[] { ' ', ',' }, 2);
                    if (subtokens.Length < 2) return;

                    string targetUser = subtokens[0];
                    string message = subtokens[1];
                    this.chatClient.SendPrivateMessage(targetUser, message);
                }
                else if ((tokens[0].Equals("\\join") || tokens[0].Equals("\\j")) && !string.IsNullOrEmpty(tokens[1]))
                {
                    string[] subtokens = tokens[1].Split(new char[] { ' ', ',' }, 2);

                    // If we are already subscribed to the channel we directly switch to it, otherwise we subscribe to it first and then switch to it implicitly
                    if (this.channelToggles.ContainsKey(subtokens[0]))
                    {
                        this.ShowChannel(subtokens[0]);
                    }
                    else
                    {
                        this.chatClient.Subscribe(new string[] { subtokens[0] });
                    }
                }
#if CHAT_EXTENDED
                else if ((tokens[0].Equals("\\nickname") || tokens[0].Equals("\\nick") ||tokens[0].Equals("\\n")) && !string.IsNullOrEmpty(tokens[1]))
                {
                    if (!doingPrivateChat)
                    {
                        this.chatClient.SetCustomUserProperties(this.selectedChannelName, this.chatClient.UserId, new Dictionary<string, object> {{"Nickname", tokens[1]}});
                    }

                }
#endif
                else
                {
                    Debug.Log("The command '" + tokens[0] + "' is invalid.");
                }
            }
            else
            {
                if (doingPrivateChat)
                {
                    this.chatClient.SendPrivateMessage(privateChatTarget, inputLine);
                }
                else
                {
                    this.chatClient.PublishMessage(this.selectedChannelName, inputLine);
                }
            }
        }

        //public void PostHelpToCurrentChannel()
        //{
        //    this.CurrentChannelText.text += HelpText;
        //}

        public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
        {
            if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
            {
                Debug.LogError(message);
            }
            else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
            {
                Debug.LogWarning(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        public void OnConnected()
        {
            if (this.channelsToJoinOnConnect != null && this.channelsToJoinOnConnect.Length > 0)
            {
                this.chatClient.Subscribe(this.channelsToJoinOnConnect, this.historyLengthToFetch);
            }

            this.connectingLabel.SetActive(false);

            this.userIdText.text = "Connected as " + this.UserName;

            this.chatPanel.gameObject.SetActive(true);

            if (this.friendsList != null && this.friendsList.Length > 0)
            {
                this.chatClient.AddFriends(this.friendsList); // Add some users to the server-list to get their status updates

                // add to the UI as well
                foreach (string _friend in this.friendsList)
                {
                    if (this.friendListUiItemtoInstantiate != null && _friend != this.UserName)
                    {
                        this.InstantiateFriendButton(_friend);
                    }

                }

            }

            if (this.friendListUiItemtoInstantiate != null)
            {
                this.friendListUiItemtoInstantiate.gameObject.SetActive(false);
            }


            this.chatClient.SetOnlineStatus(ChatUserStatus.Online); // You can set your online state (without a mesage).
        }

        public void OnDisconnected()
        {
            chatPanel.SetActive(false);
            Debug.Log("OnDisconnected()");
            this.connectingLabel.SetActive(false);
        }

        public void OnChatStateChange(ChatState state)
        {
            // use OnConnected() and OnDisconnected()
            // this method might become more useful in the future, when more complex states are being used.

            this.stateText.text = state.ToString();
        }

        public void OnSubscribed(string[] channels, bool[] results)
        {
            // in this demo, we simply send a message into each channel. This is NOT a must have!
            foreach (string channel in channels)
            {
                this.chatClient.PublishMessage(channel, "says 'hi'."); // you don't HAVE to send a msg on join but you could.

                if (this.channelToggleToInstantiate != null)
                {
                    this.InstantiateChannelButton(channel);

                }
            }

            Debug.Log("OnSubscribed: " + string.Join(", ", channels));

            /*
            // select first subscribed channel in alphabetical order
            if (this.chatClient.PublicChannels.Count > 0)
            {
                var l = new List<string>(this.chatClient.PublicChannels.Keys);
                l.Sort();
                string selected = l[0];
                if (this.channelToggles.ContainsKey(selected))
                {
                    ShowChannel(selected);
                    foreach (var c in this.channelToggles)
                    {
                        c.Value.isOn = false;
                    }
                    this.channelToggles[selected].isOn = true;
                    AddMessageToSelectedChannel(WelcomeText);
                }
            }
            */

            // Switch to the first newly created channel
            this.ShowChannel(channels[0]);
        }

        /// <inheritdoc />
        public void OnSubscribed(string channel, string[] users, Dictionary<object, object> properties)
        {
            Debug.LogFormat("OnSubscribed: {0}, users.Count: {1} Channel-props: {2}.", channel, users.Length, properties.ToStringFull());
        }

        private void InstantiateChannelButton(string channelName)
        {
            if (this.channelToggles.ContainsKey(channelName))
            {
                Debug.Log("Skipping creation for an existing channel toggle.");
                return;
            }

            Toggle cbtn = (Toggle)Instantiate(this.channelToggleToInstantiate);
            cbtn.gameObject.SetActive(true);
            cbtn.GetComponentInChildren<ChannelSelector>().SetChannel(channelName);
            cbtn.transform.SetParent(this.channelToggleToInstantiate.transform.parent, false);

            this.channelToggles.Add(channelName, cbtn);
        }

        private void InstantiateFriendButton(string friendId)
        {
            FriendItem _friendItem = Instantiate(this.friendListUiItemtoInstantiate, friendListUiItemtoInstantiate.transform.parent);
            _friendItem.gameObject.SetActive(true);

            _friendItem.FriendId = friendId;

            this.friendListItemLUT[friendId] = _friendItem;
        }


        public void OnUnsubscribed(string[] channels)
        {
            foreach (string channelName in channels)
            {
                if (this.channelToggles.ContainsKey(channelName))
                {
                    Toggle t = this.channelToggles[channelName];
                    Destroy(t.gameObject);

                    this.channelToggles.Remove(channelName);

                    Debug.Log("Unsubscribed from channel '" + channelName + "'.");

                    // Showing another channel if the active channel is the one we unsubscribed from before
                    if (channelName == this.selectedChannelName && this.channelToggles.Count > 0)
                    {
                        IEnumerator<KeyValuePair<string, Toggle>> firstEntry = this.channelToggles.GetEnumerator();
                        firstEntry.MoveNext();

                        this.ShowChannel(firstEntry.Current.Key);

                        firstEntry.Current.Value.isOn = true;
                    }
                }
                else
                {
                    Debug.Log("Can't unsubscribe from channel '" + channelName + "' because you are currently not subscribed to it.");
                }
            }
        }

        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            if (channelName.Equals(this.selectedChannelName))
            {
                // update text
                this.ShowChannel(this.selectedChannelName);
            }
        }

        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            // as the ChatClient is buffering the messages for you, this GUI doesn't need to do anything here
            // you also get messages that you sent yourself. in that case, the channelName is determinded by the target of your msg
            this.InstantiateChannelButton(channelName);

            byte[] msgBytes = message as byte[];
            if (msgBytes != null)
            {
                Debug.Log("Message with byte[].Length: " + msgBytes.Length);
            }
            if (this.selectedChannelName.Equals(channelName))
            {
                this.ShowChannel(channelName);
            }
        }

        /// <summary>
        /// New status of another user (you get updates for users set in your friends list).
        /// </summary>
        /// <param name="user">Name of the user.</param>
        /// <param name="status">New status of that user.</param>
        /// <param name="gotMessage">True if the status contains a message you should cache locally. False: This status update does not include a
        /// message (keep any you have).</param>
        /// <param name="message">Message that user set.</param>
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {

            Debug.LogWarning("status: " + string.Format("{0} is {1}. Msg:{2}", user, status, message));

            if (this.friendListItemLUT.ContainsKey(user))
            {
                FriendItem _friendItem = this.friendListItemLUT[user];
                if (_friendItem != null) _friendItem.OnFriendStatusUpdate(status, gotMessage, message);
            }
        }

        public void OnUserSubscribed(string channel, string user)
        {
            Debug.LogFormat("OnUserSubscribed: channel=\"{0}\" userId=\"{1}\"", channel, user);
        }

        public void OnUserUnsubscribed(string channel, string user)
        {
            Debug.LogFormat("OnUserUnsubscribed: channel=\"{0}\" userId=\"{1}\"", channel, user);
        }

        /// <inheritdoc />
        public void OnChannelPropertiesChanged(string channel, string userId, Dictionary<object, object> properties)
        {
            Debug.LogFormat("OnChannelPropertiesChanged: {0} by {1}. Props: {2}.", channel, userId, Extensions.ToStringFull(properties));
        }

        public void OnUserPropertiesChanged(string channel, string targetUserId, string senderUserId, Dictionary<object, object> properties)
        {
            Debug.LogFormat("OnUserPropertiesChanged: (channel:{0} user:{1}) by {2}. Props: {3}.", channel, targetUserId, senderUserId, Extensions.ToStringFull(properties));
        }

        /// <inheritdoc />
        public void OnErrorInfo(string channel, string error, object data)
        {
            Debug.LogFormat("OnErrorInfo for channel {0}. Error: {1} Data: {2}", channel, error, data);
        }

        public void AddMessageToSelectedChannel(string msg)
        {
            ChatChannel channel = null;
            bool found = this.chatClient.TryGetChannel(this.selectedChannelName, out channel);
            if (!found)
            {
                Debug.Log("AddMessageToSelectedChannel failed to find channel: " + this.selectedChannelName);
                return;
            }

            if (channel != null)
            {
                channel.Add("Bot", msg, 0); //TODO: how to use msgID?
            }
        }



        public void ShowChannel(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                return;
            }

            ChatChannel channel = null;
            bool found = this.chatClient.TryGetChannel(channelName, out channel);
            if (!found)
            {
                Debug.Log("ShowChannel failed to find channel: " + channelName);
                return;
            }

            this.selectedChannelName = channelName;
            this.currentChannelText.text = channel.ToStringMessages();
            Debug.Log("ShowChannel: " + this.selectedChannelName);

            foreach (KeyValuePair<string, Toggle> pair in this.channelToggles)
            {
                pair.Value.isOn = pair.Key == channelName ? true : false;
            }
        }
    }

}
