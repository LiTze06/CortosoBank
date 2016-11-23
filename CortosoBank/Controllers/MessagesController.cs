using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

using CortosoBank.Models;
using System.Text;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;

namespace CortosoBank
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        //Global values
        bool userLoggedIn = false;
        bool userLogin = false;
        bool createAccount;
        bool deleteAccount;
        string userAccountNo;
        List<string> loginInformation;
        Customer clientDetails;
        List<object> newUserInformation;

        List<string> userInfo = new List<string> { "Email", "Password", "FirstName", "LastName", "Age", "Balance" };
        public string[] options = new string[] { "Create Account", "Log In" };
        public string[] transactionOptions = new string[] { "withdraw", "deposit", "balance", "currency", "delete account" };


        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                var userInput = activity.Text;
                string replyToUser = $"Welcome to Cortoso Bank";


                /// ---  Store client information ----------------
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                userLoggedIn = userData.GetProperty<bool>("userLoggedIn");
                userAccountNo = userData.GetProperty<string>("userAccountNo") ?? "";
                clientDetails = userData.GetProperty<Customer>("clientDetails") ?? new Customer();
                createAccount = userData.GetProperty<bool>("createAccount");
                deleteAccount = userData.GetProperty<bool>("deleteAccount");
                newUserInformation = userData.GetProperty<List<object>>("newUserInformation") ?? new List<object>();
                loginInformation = userData.GetProperty<List<string>>("loginInformation") ?? new List<string>();
                userLogin = userData.GetProperty<bool>("userLogin");




                /// --- Clear state  ---------------------
                if (userInput.ToLower().Equals("clear"))
                {
                    replyToUser = "Your state has been cleared. ";
                    userData.SetProperty<bool>("userLogin", false);
                    userData.SetProperty<bool>("userLoggedIn", false);
                    userData.SetProperty<bool>("createAccount", false);
                    userData.SetProperty<bool>("deleteAccount", false);
                    userData.SetProperty<string>("userAccountNo", "");
                    userData.SetProperty<Customer>("clientDetails", new Customer());
                    userData.SetProperty<List<object>>("newUserInformation", new List<object>());
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);

                    /// return 
                    Activity replyMessage2 = activity.CreateReply(replyToUser);
                    await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                    return Request.CreateResponse(HttpStatusCode.OK);

                }



                /// --- Create account ---------------------
                if (userInput.ToLower().Equals("create account"))
                {
                    userData.SetProperty<bool>("createAccount", true);
                    userData.SetProperty<List<object>>("newUserInformation", new List<object>());
                    userData.SetProperty<bool>("notSendGreeting", true);
                    
                    int counter = newUserInformation.Count;
                    replyToUser = $"({counter + 1}/6): Please enter your {userInfo[counter]}"; // prompt for email
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }


                if (createAccount)
                {
                    // Get new user's account information
                    int counter = newUserInformation.Count + 1;
                    if (counter < 6)
                    {
                        newUserInformation.Add(userInput);
                        userData.SetProperty<List<object>>("newUserInformation", newUserInformation);
                        // userData.SetProperty<bool>("notSendGreeting", true);
                        //sentGreeting = false;
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        replyToUser = $"({counter + 1}/6): Please enter your {userInfo[counter]}";
                    }

                    if (counter == 6)
                    {
                        // User's all information are there. Now is to create account ! 
                        newUserInformation.Add(userInput);
                        userData.SetProperty<List<object>>("newUserInformation", newUserInformation);

                        // Get Unique AccountNo 
                        string newAccountNo = await AzureManager.AzureManagerInstance.getUniqueAccountNo();

                        Customer cust = new Customer();
                        cust.AccountNo = newAccountNo;
                        cust.Email = newUserInformation[0].ToString();
                        cust.Password = newUserInformation[1].ToString();
                        cust.FirstName = newUserInformation[2].ToString();
                        cust.LastName = newUserInformation[3].ToString();
                        cust.Age = Int32.Parse(newUserInformation[4].ToString());
                        cust.Balance = Double.Parse(newUserInformation[5].ToString());

                        string EmailAndAddress = $"{cust.Email},{cust.Password}";

                        await AzureManager.AzureManagerInstance.AddCustomer(cust);
                        replyToUser = $" Welcome to Cortoso Bank! Your account has been successfully created. Your accountNo is {newAccountNo} with the balance of ${cust.Balance}.";
                        await Conversation.SendAsync(activity, () => new TransactionDialog());

                        createAccount = false;
                        userData.SetProperty<string>("strEmailAndPassword", EmailAndAddress);
                        userData.SetProperty<bool>("userLoggedIn", true);
                        userData.SetProperty<bool>("createAccount", false);
                        userData.SetProperty<string>("userAccountNo", newAccountNo);
                        userData.SetProperty<Customer>("clientDetails", cust);
                        userData.SetProperty<List<object>>("newUserInformation", new List<object>());
                        userData.SetProperty<Customer>("clientDetails", cust);

                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        /// return 
                        Activity replyMessage2 = activity.CreateReply(replyToUser);
                        await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                        return Request.CreateResponse(HttpStatusCode.OK);

                    }
                }


                /// --- Handle log in -------------------------
                if (userInput.ToLower().Equals("log in"))
                {
                    replyToUser = "Please enter your email.";
                    userData.SetProperty<List<string>>("loginInformation", loginInformation);
                    userData.SetProperty<bool>("userLogin", true);
                 
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                if (userLogin)
                {
                    int counter = loginInformation.Count;
                    if (counter == 0)
                    {
                        loginInformation.Add(userInput); // this is to store email. 
                        replyToUser = "Please enter your password.";
                        // reset loginInformation
                        userData.SetProperty<List<string>>("loginInformation", loginInformation);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }

                    if (counter == 1)
                    {
                        loginInformation.Add(userInput); // this is to store password. 

                        string userEmail = loginInformation[0].ToString();
                        string userPassword = loginInformation[1].ToString();

                        // Authenticate user 
                        bool validEmailAndPassword = await AzureManager.AzureManagerInstance.AuthenticateCustomer(userEmail, userPassword);

                        if (validEmailAndPassword) // authenticate successfully
                        {
                            // get user data; 
                            string accountNo = await AzureManager.AzureManagerInstance.getAccountNo(userEmail, userPassword);
                            Customer cust = await AzureManager.AzureManagerInstance.getCustomerDetails(accountNo);

                            replyToUser = $"Log in successfully. Hi {cust.FirstName}!";

                            userData.SetProperty<string>("userAccountNo", accountNo);
                            userData.SetProperty<Customer>("clientDetails", cust);
                            userData.SetProperty<bool>("userLoggedIn", true);
                            userData.SetProperty<bool>("userLogin", false);
                            userData.SetProperty<List<string>>("loginInformation", new List<string>());

                            await Conversation.SendAsync(activity, () => new TransactionDialog());
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }
                        else
                        {
                            replyToUser = "Invalid Email and Password.";
                            userData.SetProperty<bool>("userLoggedIn", false);
                            userData.SetProperty<bool>("userLogin", false);
                            userData.SetProperty<List<string>>("loginInformation", new List<string>());
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }
                    }

                }


                //// --- User haven't log in ---------------------------

                if (!createAccount && !userLogin && !userLoggedIn && !userInput.ToLower().Equals("clear") && !userInput.ToLower().Equals("log in") && !userInput.ToLower().Equals("create account") && !userInput.ToLower().Equals("delete account"))
                {
                   
                    /// Luis Intent 
                    LuisIntentObject luisIntentObject = await GetEntityFromLUIS(userInput);
                    switch (luisIntentObject.topScoringIntent.intent)
                    {
                        case "getHelp":
                            // ##  Introducting Page ##
                            Activity replyToConversation = activity.CreateReply();
                            replyToConversation.Recipient = activity.From;
                            replyToConversation.Type = "message";
                            replyToConversation.Attachments = new List<Attachment>();

                            // CardButtons
                            var actions = new List<CardAction>();
                            for (int i = 0; i < options.Length; i++)
                            {
                                actions.Add(new CardAction
                                {
                                    Title = $"{options[i]}",
                                    Value = $"{options[i]}",
                                    Type = ActionTypes.ImBack
                                });
                            }

                            // CardImage
                            List<CardImage> cardImages = new List<CardImage>();
                            cardImages.Add(new CardImage(url: "https://irp-cdn.multiscreensite.com/d0e68b97/dms3rep/multi/mobile/icon_002-300x300.png"));


                            // Reply with thumbnail card
                            replyToConversation.Attachments.Add(
                                 new ThumbnailCard
                                 {
                                     Title = $"Welcome to Cortoso Bank",
                                     Subtitle = "How can i help you?",
                                     Images = cardImages,
                                     Buttons = actions
                                 }.ToAttachment()
                            );
                            await connector.Conversations.SendToConversationAsync(replyToConversation);
                            return Request.CreateResponse(HttpStatusCode.OK);
                            break;

                        case "getCurrencyRate":
                            replyToUser = await GetCurrencyRate(userInput);
                            break;

                        case "None":
                            // ##  Introducting Page ##
                            Activity replyToConversation2 = activity.CreateReply();
                            replyToConversation2.Recipient = activity.From;
                            replyToConversation2.Type = "message";
                            replyToConversation2.Attachments = new List<Attachment>();

                            // CardButtons
                            var actions2 = new List<CardAction>();
                            for (int i = 0; i < options.Length; i++)
                            {
                                actions2.Add(new CardAction
                                {
                                    Title = $"{options[i]}",
                                    Value = $"{options[i]}",
                                    Type = ActionTypes.ImBack
                                });
                            }

                            // CardImage
                            List<CardImage> cardImages2 = new List<CardImage>();
                            cardImages2.Add(new CardImage(url: "https://irp-cdn.multiscreensite.com/d0e68b97/dms3rep/multi/mobile/icon_002-300x300.png"));


                            // Reply with thumbnail card
                            replyToConversation2.Attachments.Add(
                                 new ThumbnailCard
                                 {
                                     Title = $"Welcome to Cortoso Bank",
                                     Subtitle = "How can i help you?",
                                     Images = cardImages2,
                                     Buttons = actions2
                                 }.ToAttachment()
                            );
                            await connector.Conversations.SendToConversationAsync(replyToConversation2);
                            return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    
                }


                /// handle delete account. If user want to delete account, he or she must already log in first. 
                if (userLoggedIn && userInput.ToLower().Equals("delete account"))
                {
                    await AzureManager.AzureManagerInstance.DeleteCustomer(clientDetails);

                    userData.SetProperty<bool>("userLoggedIn", false);
                    replyToUser = "Your account has been successfully deleted.";
                    userAccountNo = "";

                    Activity replyMessage2 = activity.CreateReply(replyToUser);
                    await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }



                /// --- User logged in ---------------------------
                if (userLoggedIn & !userInput.ToLower().Equals("clear"))
                {
                 
                    /// Luis Intent 
                    LuisIntentObject luisIntentObject = await GetEntityFromLUIS(userInput);
                    switch (luisIntentObject.topScoringIntent.intent)
                    {
                        case "viewbalance":
                            double Balance = await ViewBalance(userAccountNo);
                            replyToUser = $"Your balance is ${string.Format("{0:0.00}", Balance)}.";
                            break;

                        case "getHelp":
                            await Conversation.SendAsync(activity, () => new TransactionDialog());
                            replyToUser = $"USER :I am here to help {userLoggedIn}";
                            return Request.CreateResponse(HttpStatusCode.OK);

                        case "withdraw":
                            bool WithDrawSuccess = await Withdraw(clientDetails, userInput);
                            switch (WithDrawSuccess)
                            {
                                case true:
                                    userData.SetProperty<Customer>("clientDetails", clientDetails);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    replyToUser = $"Withdraw successfully. Your new balance is ${string.Format("{0:0.00}", clientDetails.Balance)}";
                                    break;
                                default:
                                    replyToUser = "Do you want to withdraw money? If yes please type 'withdraw amount'";
                                    break;
                            }
                            break;

                        case "deposit":
                            bool DepositSuccess = await Deposit(clientDetails, userInput);
                            switch (DepositSuccess)
                            {
                                case true:
                                    userData.SetProperty<Customer>("clientDetails", clientDetails);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    replyToUser = $"Deposited Successfully. Your new balance is ${string.Format("{0:0.00}", clientDetails.Balance)}";
                                    break;
                                default:
                                    replyToUser = "Do you want to deposit money? If yes please type 'deposit amount'";
                                    break;
                            }
                            break;
                        case "getCurrencyRate":
                            replyToUser = await GetCurrencyRate(userInput);
                            break;
                        case "transfer":
                            replyToUser = "transfer";
                            break;
                        default:
                            if (userInput.ToLower().Contains("convert"))
                            {
                                replyToUser = await GetCurrencyRate(userInput);
                            }
                            else
                            {
                                await Conversation.SendAsync(activity, () => new TransactionDialog());
                                replyToUser = $"Sorry, i am not getting you. Please type 'help' for more information. {userLoggedIn}";
                            }
                            break;
                    }

                }




                /// --- Create a reply message ----------------
                Activity replyMessage = activity.CreateReply(replyToUser);
                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                await connector.Conversations.ReplyToActivityAsync(replyMessage);

            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }









        /// <summary>
        /// To deposit money
        /// </summary>
        /// <param name="userInput"></param>
        /// <returns> true if withdraw successfully, otherwise false</returns>
        private async Task<bool> Deposit(Customer clientDetails, string userInput)
        {
            bool containsInt = userInput.Any(char.IsDigit);

            if (containsInt)
            {
                var valueToBeConvert = (from t in userInput
                                        where char.IsDigit(t) || Convert.ToString(t).Equals(".")
                                        select t).ToArray();

                string amountString = "";
                for (int i = 0; i < valueToBeConvert.Length; i++)
                {
                    amountString = amountString + valueToBeConvert[i];
                }
                var amountToBeWithdraw = Double.Parse(amountString);
                double newAmount = await AzureManager.AzureManagerInstance.DepositMoney(clientDetails, amountToBeWithdraw);
                return true;
            }
            return false;
        }



        /// <summary>
        /// To withdraw money
        /// </summary>
        /// <param name="userInput"></param>
        /// <returns> true if withdraw successfully, otherwise false</returns>
        private async Task<bool> Withdraw(Customer clientDetails, string userInput)
        {
            bool containsInt = userInput.Any(char.IsDigit);

            if (containsInt)
            {
                var valueToBeConvert = (from t in userInput
                                        where char.IsDigit(t) || Convert.ToString(t).Equals(".")
                                        select t).ToArray();

                string amountString = "";
                for (int i = 0; i < valueToBeConvert.Length; i++)
                {
                    amountString = amountString + valueToBeConvert[i];
                }
                var amountToBeWithdraw = Double.Parse(amountString);
                double newAmount = await AzureManager.AzureManagerInstance.WithdrawMoney(clientDetails, amountToBeWithdraw);
                return true;
            }
            return false;
        }


        /// <summary>
        /// To view balance
        /// </summary>
        /// <param name="accountNo"></param>
        /// <returns></returns>
        private async Task<double> ViewBalance(string accountNo)
        {
            double Balance = await AzureManager.AzureManagerInstance.getBalance(accountNo);
            return Balance;
        }


        /// <summary>
        /// get currency rate using Fixer's API
        /// </summary>
        /// <param name="userInput"></param>
        /// <returns></returns>
        private async Task<string> GetCurrencyRate(string userInput)
        {
            HttpClient client = new HttpClient();
            string currencyURL = "http://api.fixer.io/latest";
            string fromCurrency = userInput.Substring(8, 3);
            string toCurrency = userInput.Substring(15, 3).ToUpper();
            string currencyResponse = await client.GetStringAsync(new Uri(currencyURL + "?base=" + fromCurrency));

            //deserialize 
            CurrencyObject currencyObject;
            currencyObject = JsonConvert.DeserializeObject<CurrencyObject>(currencyResponse);

            string currencyBase = currencyObject.@base;
            string currencyDate = currencyObject.date;
            Rates currencyRates = currencyObject.rates;

            ExchangeRate exRate = new ExchangeRate(fromCurrency, toCurrency, currencyObject);
            double rates = exRate.getCurrencyRate();
            string replyToUser = "Current Rates from " + currencyBase + " to " + toCurrency + " is " + rates;

            return replyToUser;
        }


        /// <summary>
        ///  Manipulate JSON file 
        /// </summary>
        /// <param name="Query"></param>
        /// <returns>LuisIntentObject</returns>
        private static async Task<LuisIntentObject> GetEntityFromLUIS(string userInput)
        {

            string Query = Uri.EscapeDataString(userInput);
            LuisIntentObject luisObject = new LuisIntentObject();

            HttpClient client = new HttpClient();
            string luisRequestUrl = "https://api.projectoxford.ai/luis/v2.0/apps/8f510ad0-4e70-4b25-9fde-d136dba07d7f?subscription-key=719459cce3b34a8fbf4aeee23cde4be0&q=";
            string luisResponse = await client.GetStringAsync(new Uri(luisRequestUrl + Query));

            //deserialize 
            LuisIntentObject luisIntentObject;
            luisIntentObject = JsonConvert.DeserializeObject<LuisIntentObject>(luisResponse);

            return luisIntentObject;
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