using System;
using System.Linq;
using System.Xml;
using System.Collections.Specialized;

namespace Vk_Manager
{
    class vk_api
    {
        static String accessToken;

        public vk_api(String token)
        {
            accessToken = token;
        }

        private XmlDocument ExecuteCommand(String name, NameValueCollection qs)
        {
            XmlDocument result = new XmlDocument();
            try
            {
                result.Load(String.Format("https://api.vkontakte.ru/method/{0}.xml?access_token={1}&{2}", name, accessToken, String.Join("&", from item in qs.AllKeys select item + "=" + qs[item])));
                return result;
            }
            catch 
            {
                return null;
            }
        }

        public XmlDocument SendMessage(Int32 id, String messages)
        {
            NameValueCollection param = new NameValueCollection();
            param["uid"] = id.ToString();
            param["chat_id"] = id.ToString();
            param["message"] = messages;

            XmlDocument result = new XmlDocument();
            result.Load(String.Format("https://api.vkontakte.ru/method/{0}.xml?access_token={1}&{2}", "messages.send", accessToken, String.Join("&", from item in param.AllKeys select item + "=" + param[item])));
            return result;
        }

        public XmlDocument SendMessageWithCaptcha(Int32 id, String messages, String captcha_key, String captcha_sid)
        {
            NameValueCollection param = new NameValueCollection();
            param["uid"] = id.ToString();
            param["chat_id"] = id.ToString();
            param["message"] = messages;
            param["captcha_sid"] = captcha_sid;
            param["captcha_key"] = captcha_key;
            XmlDocument result = new XmlDocument();
            result.Load(String.Format("https://api.vkontakte.ru/method/{0}.xml?access_token={1}&{2}", "messages.send", accessToken, String.Join("&", from item in param.AllKeys select item + "=" + param[item])));
            return result;
        }

        public XmlDocument GetFriends()
        {
            NameValueCollection param = new NameValueCollection();
            param["fields"] = "uid, first_name, last_name, photo_big";
            param["count"] = "10";
            param["order"] = "hints";
            return ExecuteCommand("friends.get", param);
        }

        public XmlDocument GetNewMessages()
        {
            NameValueCollection param = new NameValueCollection();
            param["out"] = "0";
            param["filters"] = "1";
            param["count"] = "100";
            return ExecuteCommand("messages.get", param);
        }
    }
}
