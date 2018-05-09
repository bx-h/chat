using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using App2.weixinInit;
using App2.weixinContact;
using Newtonsoft.Json.Linq;
using App2.weixinMessage;
using System.ComponentModel;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace App2
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ContactView ContactView = new ContactView();
        string redirect_uri;
        Error cookie;
        string cookie_str;
        Init weChat;
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            redirect_uri = (string)e.Parameter;
            Debug.WriteLine(redirect_uri);
        }

        private async Task Init()
        {
            BaseRequest baseRequest = new BaseRequest(cookie.wxuin, cookie.wxsid, cookie.skey);
            JObject jsonObj = new JObject();
            jsonObj.Add("BaseRequest", JObject.FromObject(baseRequest));
            string json = jsonObj.ToString().Replace("\r\n", "");

            string uri = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxinit?r=" + Time.Now() + "&lang=ch_ZN&pass_ticket=" + cookie.pass_ticket;

            string result = await Post.Get_Response_Str(uri, json);

            Debug.WriteLine(result);

            weChat = wxInit.Get_Init(result);
            cookie.syncKey = weChat.SyncKey;

            foreach (var member in weChat.ContactList)
            {
                if (!member.UserName[1].Equals('@'))
                {
                    FriendList friend = new FriendList();
                    friend.UserName = member.UserName;
                    friend.NickName = member.NickName;
                    friend.dialog = "";
                    ContactView.AllItems.Add(friend);
                }
            }

        }

        private async Task Get_Contact()
        {
            string uri = "http://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact" +
                "?&r=" + Time.Now();
            cookie_str = "webwx_data_ticket=" + cookie.webwx_data_ticket + "; wxsid=" + cookie.wxsid + "; wxuin=" + cookie.wxuin;

            var myClient = new HttpClient();
            var myRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            myRequest.Headers.Add("Cookie", cookie_str);
            var response = await myClient.SendAsync(myRequest);
            string result = await response.Content.ReadAsStringAsync();

            Contact contact = wxContact.Get_Contact(result);

            
           
        }


        private async Task Get_Cookie()
        {
            string uri = redirect_uri + "&fun=new";

            HttpClientHandler myHandler = new HttpClientHandler();
            myHandler.AllowAutoRedirect = false;
            HttpClient myClient = new HttpClient(myHandler);
            var myRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await myClient.SendAsync(myRequest);
            string result = await response.Content.ReadAsStringAsync();

            cookie = Cookie.Get_Cookie(result);
            
            string[] temp = myHandler.CookieContainer.GetCookieHeader(new Uri(uri)).Split(new char[] { ',', ';' });
            foreach (string c in temp)
            {
                if (c.Contains("webwx_data_ticket"))
                {
                    cookie.webwx_data_ticket = c.Split('=')[1];
                    break;
                }
            }
        }

        private async Task HeartBeat()
        {
            string uri = "https://webpush.wx2.qq.com/cgi-bin/mmwebwx-bin/synccheck" +
                "?skey=" + cookie.skey +
                "&r=" + Time.Now() +
                "&sid=" + cookie.wxsid +
                "&uin=" + cookie.wxuin +
                "&deviceid=" + "e123456789012345" +
                "&synckey=" + cookie.syncKey.get_urlstring() +
                "&_=" + Time.Now();
            cookie_str = "webwx_data_ticket=" + cookie.webwx_data_ticket + "; wxsid=" + cookie.wxsid + "; wxuin=" + cookie.wxuin;
            var myClient = new HttpClient();
            var myRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            myRequest.Headers.Add("Cookie", cookie_str);
            var response = await myClient.SendAsync(myRequest);
            string result = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(result);

            string result_str = result.Split('=')[1];

            string selector = result_str.Split('"')[3];

            Debug.WriteLine(selector);

            if(!selector.Equals("0"))
            {
                Get_Message();
            }
        }

        private async void Get_Message()
        {
            string uri = "http://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsync" +
                "?pass_ticket=" + cookie.pass_ticket +
                "&r=" + Time.Now();

            BaseRequest baseRequest = new BaseRequest(cookie.wxuin, cookie.wxsid, cookie.skey);
            JObject jsonObj = new JObject();
            jsonObj.Add("BaseRequest", JObject.FromObject(baseRequest));
            jsonObj.Add("SyncKey", JObject.FromObject(cookie.syncKey));
            jsonObj.Add("rr", Time.Now());

            string json = jsonObj.ToString().Replace("\r\n", "");

            string result = await Post.Get_Response_Str(uri, json);


            var message = Message.FromJson(result);

            cookie.syncKey = message.SyncKey;

            foreach(var user in message.AddMsgList)
            {
                bool flag = false;
                for(int i = 0; i < ContactView.AllItems.Count; i++)
                {
                    if(user.FromUserName == ContactView.AllItems[i].UserName)
                    {
                        ContactView.AllItems[i].dialog += "对方:" + user.Content + "\n";
                        flag = true;
                        break;
                    }
                }
                if(!flag)
                {
                    FriendList friend = new FriendList();
                    friend.UserName = user.FromUserName;
                    friend.NickName = "nick";
                    friend.dialog += "对方:" + user.Content + "\n";
                    ContactView.AllItems.Add(friend);
                }
            }

            Debug.WriteLine("读取消息");
            Debug.WriteLine("BaseResponse.Ret:" + message.BaseResponse.Ret);
            Debug.WriteLine("AddMsgCount:" + message.AddMsgCount);
            foreach (var a in message.AddMsgList)
            {
                Debug.WriteLine(a.Content);
            }

            Debug.WriteLine("ModContactCount:" + message.ModContactCount);
            Debug.WriteLine("DelContactCount:" + message.DelContactCount);
            Debug.WriteLine("ModChatRoomMemberCount:" + message.ModChatRoomMemberCount);
        }

        private async void background(object sender, RoutedEventArgs e)
        {
            await Get_Cookie();
            await Init();
            await Get_Contact();
            await Listen();

        }

        private async Task Listen()
        {
            while(true)
            {
                await Task.Delay(500);
                await HeartBeat();
            }
        }

        FriendList friend;
        private void Get_Dialog(object sender, ItemClickEventArgs e)
        {
            friend = (FriendList)e.ClickedItem;
            dialog.Text = friend.dialog;
        }
        

        private async void Send_Message(object sender, RoutedEventArgs e)
        {
            if(friend != null)
            {
                string uri = "http://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg" +
                    "?sid=" + cookie.wxsid +
                    "&skey=" + cookie.skey +
                    "&pass_ticket=" + cookie.pass_ticket +
                    "&r=" + Time.Now();

                BaseRequest baseRequest = new BaseRequest(cookie.wxuin, cookie.wxsid, cookie.skey);
                JObject jsonObj = new JObject();
                jsonObj.Add("BaseRequest", JObject.FromObject(baseRequest));

                SendMsg msg = new SendMsg();
                msg.FromUserName = weChat.User.UserName;
                msg.ToUserName = friend.UserName;
                msg.Type = 1;
                msg.Content = send.Text;
                msg.ClientMsgId = Time.Now();
                msg.LocalID = Time.Now();
                jsonObj.Add("Msg", JObject.FromObject(msg));



                string json = jsonObj.ToString().Replace("\r\n", "");

                string result = await Post.Get_Response_Str(uri, json);
                
                for (int i = 0; i < ContactView.AllItems.Count; i++)
                {
                    if (friend.UserName == ContactView.AllItems[i].UserName)
                    {
                        ContactView.AllItems[i].dialog += "我:" + send.Text + "\n";

                    }
                }
            }
            
        }
    }
}

