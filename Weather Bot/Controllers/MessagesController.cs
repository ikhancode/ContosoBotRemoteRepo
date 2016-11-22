using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Weather_Bot.Models;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using Weather_Bot.DataModels;

namespace Weather_Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
   
        string output;
        string userInput;

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                WeatherObject.RootObject rootObject;
                HttpClient client = new HttpClient();
                string request = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=NZD"));
                rootObject = JsonConvert.DeserializeObject<WeatherObject.RootObject>(request);

                string openingMessage = "This is CoBAI";
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    openingMessage = "This is CoBAI again";

                }

                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                bool isRequest = true;
                var userInput = activity.Text;

                if (userInput.ToLower().Contains("clear"))
                {
                    openingMessage = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    isRequest = false;
                }
                Activity resp = activity.CreateReply(openingMessage);
                await connector.Conversations.ReplyToActivityAsync(resp);

                //Implement currency rate here -----------------------------------------------------

                if (userInput.ToLower().Contains("currency for"))
                {
                    string[] value = userInput.Split(' ');
                    double ZAR = rootObject.rates.ZAR;
                    double HKD = rootObject.rates.HKD;
                    double AUD = rootObject.rates.AUD;
                    double USD = rootObject.rates.USD;
                    double GBP = rootObject.rates.GBP;
                    double CAD = rootObject.rates.CAD;
                    double JPY = rootObject.rates.JPY;
                    double BGN = rootObject.rates.BGN;

                    if (value[2].ToLower() == "zar") { output = ZAR + " " + value[2].ToUpper(); }
                    if (value[2].ToLower() == "hkd") { output = HKD + " " + value[2].ToUpper(); }
                    if (value[2].ToLower() == "aud") { output = AUD + " " + value[2].ToUpper(); }
                    if (value[2].ToLower() == "usd") { output = USD + " " + value[2].ToUpper(); }
                    if (value[2].ToLower() == "gbp") { output = GBP + " " + value[2].ToUpper(); }
                    if (value[2].ToLower() == "cad") { output = CAD + " " + value[2].ToUpper(); }
                    if (value[2].ToLower() == "jpy") { output = JPY + " " + value[2].ToUpper(); }
                    if (value[2].ToLower() == "bgn") { output = BGN + " " + value[2].ToUpper(); }

                    Activity replyToConversation = activity.CreateReply("Rate per NZD is: ");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://cdn4.iconfinder.com/data/icons/aiga-symbol-signs/441/aiga_cashier-128.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://www.google.co.nz/webhp?sourceid=chrome-instant&rlz=1C1CHZL_enNZ703NZ703&ion=1&espv=2&ie=UTF-8#q=Convert+Currency+",
                        Type = "openUrl",
                        Title = "Visit Google's currency converter"
                    };
                    cardButtons.Add(plButton);
                    HeroCard plCard = new HeroCard()
                    {
                        Title = output,
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    var reply1 = await connector.Conversations.SendToConversationAsync(replyToConversation);
                }

                if (userInput.ToLower().Equals("contoso"))
                {
                    Activity replyToConversation = activity.CreateReply("Bank information");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://cdn2.iconfinder.com/data/icons/ios-7-style-metro-ui-icons/512/MetroUI_iCloud.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "https://bancocontoso.azurewebsites.net/",
                        Type = "openUrl",
                        Title = "Contoso Bank Website"
                    };
                    cardButtons.Add(plButton);
                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Visit Contoso Bank",
                        Subtitle = "The Contoso Bank Website is here",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);
                }
                //Implementing currency rate ends here -----------------------------------------------------
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
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
