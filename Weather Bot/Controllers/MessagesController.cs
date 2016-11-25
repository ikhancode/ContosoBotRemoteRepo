using System;
using System.Net;
using System.Web;
using System.Linq;
using CoBAI_Bot;
using Newtonsoft.Json;
using System.Net.Http;
using System.Web.Http;
using CoBAI_Bot.Models;
using CoBAI_Bot.DataModels;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Web.Http.Description;

namespace CoBAI_Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private string output;
        public string result;
        public bool currency = false;
        public bool info = false;

        private static async Task<RootObject> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            RootObject Data = new RootObject();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://api.projectoxford.ai/luis/v2.0/apps/78b7f8a7-fed1-49ff-9787-18d8ef429a94?subscription-key=0c37e86de7cc480089cb3bb4c19db133&q=" + Query + "&verbose=true";
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<RootObject>(JsonDataResponse);
                }
            }
            return Data;

        }
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                string output = "Hello";
                var userInput = activity.Text;

                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    if (userInput.ToLower().Contains("hello"))
                    {
                        output = "Hello again";
                    }
                    else
                    {
                        output = "Hmmm.. I'm not sure what you meant..Please refer to our website to know more about CoBAI";
                    }
                }
                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                bool requested = true;
                string luisEntityVal;
                RootObject luisEntity = await GetEntityFromLUIS(activity.Text);
                if (luisEntity.intents.Count() > 0)
                {
                    switch (luisEntity.intents[0].intent)
                    {
                        case "convert to gbp":
                            currency = true;
                            luisEntityVal = await GetConversion(luisEntity.entities[0].entity);
                            break;
                        case "convert to aud":
                            currency = true;
                            luisEntityVal = await GetConversion(luisEntity.entities[0].entity);
                            break;
                        case "bank info":
                            info = true;
                            break;
                        default:
                            luisEntityVal = "Sorry, I am not getting you...";
                            break;
                    }
                }

                if (currency == true)
                {
                    Activity replyToConversation = activity.CreateReply("The exchange rate for $1 NZD is " + result);
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://cdn4.iconfinder.com/data/icons/aiga-symbol-signs/441/aiga_cashier-512.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://www.google.co.nz/webhp?sourceid=chrome-instant&rlz=1C1CHZL_enNZ703NZ703&ion=1&espv=2&ie=UTF-8#q=Currency+converter",
                        Type = "openUrl",
                        Title = "Use Converter "
                    };
                    cardButtons.Add(plButton);
                    HeroCard plCard = new HeroCard()
                    {
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    var replyToConv = await connector.Conversations.SendToConversationAsync(replyToConversation);
                    return Request.CreateResponse(HttpStatusCode.OK);

                }
                else if (info == true)
                {
                    Activity replyToConversation = activity.CreateReply("Visit our website for all the information you need");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://s13.postimg.org/ajz1l9ll3/logggo.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://bancocontoso.azurewebsites.net/",
                        Type = "openUrl",
                        Title = "Contoso Bank"
                    };
                    cardButtons.Add(plButton);
                    HeroCard plCard = new HeroCard()
                    {
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userInput.ToLower().Contains("clear"))
                {
                    output = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    requested = false;

                }

                if (userInput.ToLower().Contains("delete my records"))
                {

                    List<Timeline> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    foreach (Timeline t in timelines)
                    {
                        if (activity.From.Name.Equals(t.RealName))
                        {
                            await AzureManager.AzureManagerInstance.DeleteTimeline(t);
                        }
                    }
                    Activity repl = activity.CreateReply("It's deleted!");
                    await connector.Conversations.ReplyToActivityAsync(repl);
                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                if (userInput.ToLower().Contains("get my records"))
                {
                    List<Timeline> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    output = "";
                    foreach (Timeline t in timelines)
                    {
                        if (activity.From.Name.Equals(t.RealName))
                        {
                            output += "CONTOSO BANK \n\n" + "UserName: " + t.Name + "\n\n" + "Cheque " + t.Cheque + "\n\n" + " Savings: " + t.Savings + "\n\n";
                        }
                    }
                    requested = false;

                }

                if (userInput.ToLower().Contains("update name to"))
                {
                    var name = userInput.Split(' ');
                    List<Timeline> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    output = "";
                    foreach (Timeline t in timelines)
                    {
                        if (activity.From.Name.Contains(t.RealName))
                        {
                            t.Name = name[3];
                            await AzureManager.AzureManagerInstance.UpdateTimeline(t);
                        }
                    }
                    Activity repl = activity.CreateReply("It's updated!");
                    await connector.Conversations.ReplyToActivityAsync(repl);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userInput.ToLower().Contains("add account"))
                {
                    requested = true;
                    var userInfo = userInput.Split(' ');
                    var nickName = userInfo[2];
                    var cheque = userInfo[3];
                    var savings = userInfo[4];
                    Timeline timeline = new Timeline()
                    {
                        RealName = activity.From.Name,
                        Name = nickName,
                        Cheque = cheque,
                        Savings = savings,
                        Date = DateTime.Now
                    };

                    await AzureManager.AzureManagerInstance.AddTimeline(timeline);

                    output = "New account added [" + timeline.Date + "]";
                }

                Activity crtReply = activity.CreateReply(output);
                await connector.Conversations.ReplyToActivityAsync(crtReply);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<string> GetConversion(string StockSymbol)
        {
            CurrencyObject.RootObject rootObject;
            HttpClient client = new HttpClient();
            string apiReq = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=NZD"));

            rootObject = JsonConvert.DeserializeObject<CurrencyObject.RootObject>(apiReq);

            double ZAR = rootObject.rates.ZAR;
            double HKD = rootObject.rates.HKD;
            double AUD = rootObject.rates.AUD;
            double USD = rootObject.rates.USD;
            double GBP = rootObject.rates.GBP;
            double CAD = rootObject.rates.CAD;
            double JPY = rootObject.rates.JPY;
            double BGN = rootObject.rates.BGN;

            if (StockSymbol.ToLower() == "zar") { result = ZAR + " in " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "hkd") { result = HKD + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "aud") { result = AUD + " in  " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "usd") { result = USD + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "gbp") { result = GBP + " in  " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "cad") { result = CAD + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "jpy") { result = JPY + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "bgn") { result = BGN + " " + StockSymbol.ToUpper(); }

            return result;
        }
        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
            return null;
        }

    }
}