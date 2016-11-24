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
        private string endOutput;
        //private string userMessage;
        public string result;
        public bool currency = false;
        public bool info = false;

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                string endOutput = "Hello";

                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hello again";
                }

                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                bool requested = true;
                var userMessage = activity.Text;
                string StockRateString;
                RootObject StLUIS = await GetEntityFromLUIS(activity.Text);
                if (StLUIS.intents.Count() > 0)
                {
                    switch (StLUIS.intents[0].intent)
                    {
                        case "convert to aud":
                            currency = true;
                            StockRateString = await GetConversion(StLUIS.entities[0].entity);
                            break;
                        case "convert to gbp":
                            currency = true;
                            StockRateString = await GetConversion(StLUIS.entities[0].entity);
                            break;
                        case "bank info": 
                            info = true;
                            break;
                        default:
                            StockRateString = "Sorry, I am not getting you...";
                            break;
                    }
                }
                else
                {
                    StockRateString = "Sorry, I am not getting you...";
                }

                if (currency == true)
                {
                    Activity replyToConversation = activity.CreateReply("The exchange rate for $1 NZD is:");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://<ImageUrl1>"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://www.google.co.nz/webhp?sourceid=chrome-instant&rlz=1C1CHZL_enNZ703NZ703&ion=1&espv=2&ie=UTF-8#q=Currency+converter",
                        Type = "openUrl",
                        Title = "Online converter"
                    };
                    cardButtons.Add(plButton);
                    HeroCard plCard = new HeroCard()
                    {
                        Title = result,
                        Subtitle = "",
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
                    Activity replyToConversation = activity.CreateReply("Visit Bank");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://cdn4.iconfinder.com/data/icons/aiga-symbol-signs/441/aiga_cashier-128.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://bancocontoso.azurewebsites.net/",
                        Type = "openUrl",
                        Title = "Bank website"
                    };
                    cardButtons.Add(plButton);
                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Visit Bank",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    requested = false;

                }

                if (userMessage.ToLower().Contains("delete my records"))
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

                if (userMessage.ToLower().Contains ("get my records"))
                {
                    List<Timeline> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    endOutput = "";
                    foreach (Timeline t in timelines)
                    {
                        if (activity.From.Name.Equals(t.RealName))
                        {
                            endOutput += "[" + t.Date + "] People: " + t.Name + ", Balance " + t.Cheque + "\n\n";
                        }
                    }
                    requested = false;
                    
                }

                if (userMessage.ToLower().Contains("update name to"))
                {
                    var name = userMessage.Split(' ');
                    List<Timeline> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    endOutput = "";
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

                if (userMessage.ToLower().Contains("add account"))
                {
                    var userInfo = userMessage.Split(' ');
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

                    requested = false;

                    endOutput = "New account added [" + timeline.Date + "]";
                }

                Activity crtReply = activity.CreateReply(endOutput);
                await connector.Conversations.ReplyToActivityAsync(crtReply);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

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

        private async Task<string> GetConversion(string StockSymbol)
        {
            CurrencyObject.RootObject rootObject;
            HttpClient client = new HttpClient();
            string x = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=NZD"));

            rootObject = JsonConvert.DeserializeObject<CurrencyObject.RootObject>(x);

            double ZAR = rootObject.rates.ZAR;
            double HKD = rootObject.rates.HKD;
            double AUD = rootObject.rates.AUD;
            double USD = rootObject.rates.USD;
            double GBP = rootObject.rates.GBP;
            double CAD = rootObject.rates.CAD;
            double JPY = rootObject.rates.JPY;
            double BGN = rootObject.rates.BGN;

            if (StockSymbol.ToLower() == "zar") { result = ZAR + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "hkd") { result = HKD + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "aud") { result = AUD + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "usd") { result = USD + " " + StockSymbol.ToUpper(); }
            if (StockSymbol.ToLower() == "gbp") { result = GBP + " " + StockSymbol.ToUpper(); }
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