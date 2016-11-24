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
        bool userLoggedIn;
        bool userLogin;
        bool createAccount;
        bool deleteAccount;
        string userAccountNo;
        Customer clientDetails;
        List<string> loginInformation;
        List<object> newUserInformation;

        List<string> userInfo = new List<string> { "Email", "Password", "FirstName", "LastName", "Age", "Balance" };
        public string[] options = new string[] { "Create Account", "Log In", "Currency" };
        public string[] transactionOptions = new string[] { "withdraw", "deposit", "balance", "currency", "delete account", "log out" };

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
                string replyToUser = $"Cortoso Bank";


                /// ---  Store client information ----------------
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                userLoggedIn = userData.GetProperty<bool>("userLoggedIn");
                userLogin = userData.GetProperty<bool>("userLogin");
                createAccount = userData.GetProperty<bool>("createAccount");
                deleteAccount = userData.GetProperty<bool>("deleteAccount");
                userAccountNo = userData.GetProperty<string>("userAccountNo") ?? "";
                clientDetails = userData.GetProperty<Customer>("clientDetails") ?? new Customer();
                newUserInformation = userData.GetProperty<List<object>>("newUserInformation") ?? new List<object>();
                loginInformation = userData.GetProperty<List<string>>("loginInformation") ?? new List<string>();
              


                /// --- Clear state  ----------------------
                if (userInput.ToLower().Equals("clear") || userInput.ToLower().Equals("log out"))
                {
                    replyToUser = "Your states have been cleared.";
                    userData.SetProperty<bool>("userLogin", false);
                    userData.SetProperty<bool>("userLoggedIn", false);
                    userData.SetProperty<bool>("createAccount", false);
                    userData.SetProperty<bool>("deleteAccount", false);
                    userData.SetProperty<string>("userAccountNo", "");
                    userData.SetProperty<Customer>("clientDetails", new Customer());
                    userData.SetProperty<List<object>>("newUserInformation", new List<object>());
                    userData.SetProperty<List<string>>("loginInformation", new List<string>());
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);

                    // ##  Introduction Page ##
                    Activity replyToConversation = activity.CreateReply(replyToUser);
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
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);
                    return Request.CreateResponse(HttpStatusCode.OK);

                }


                /// --- Create account ---------------------
                if (userInput.ToLower().Equals("create account"))
                {
                    int counter = newUserInformation.Count;
                    replyToUser = $"({counter + 1}/6): Please enter your {userInfo[counter]}"; // prompt for email
                    userData.SetProperty<bool>("createAccount", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    
                    /// return 
                    Activity replyMessage2 = activity.CreateReply(replyToUser);
                    await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                    return Request.CreateResponse(HttpStatusCode.OK);

                }


                if (createAccount)
                {
                    // Get new user's account information
                    int counter = newUserInformation.Count + 1;
                    if (counter < 6)
                    {
                        newUserInformation.Add(userInput);
                        userData.SetProperty<List<object>>("newUserInformation", newUserInformation);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        replyToUser = $"({counter + 1}/6): Please enter your {userInfo[counter]}";

                        /// return 
                        Activity replyMessage2 = activity.CreateReply(replyToUser);
                        await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                        return Request.CreateResponse(HttpStatusCode.OK);

                    }

                    if (counter == 6)
                    {
                        // User's all information are there. Now is to create account ! 
                        newUserInformation.Add(userInput);
                       
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

                        await AzureManager.AzureManagerInstance.AddCustomer(cust);
                        
                        // ##  User Created Account ##

                        createAccount = false;
                        userData.SetProperty<bool>("userLoggedIn", true);
                        userData.SetProperty<bool>("createAccount", false);
                        userData.SetProperty<string>("userAccountNo", newAccountNo);
                        userData.SetProperty<Customer>("clientDetails", cust);
                        userData.SetProperty<List<object>>("newUserInformation", newUserInformation);
                        
                        // ##  Account created page ##
                        Activity replyToConversation = activity.CreateReply(replyToUser);
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();

                        // CardImage
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "http://stock.wikimini.org/w/images/9/95/Gnome-stock_person-avatar-profile.png"));

                        // Reply with Hero card
                        replyToConversation.Attachments.Add(
                             new HeroCard
                             {
                                 Title = "Your details",
                                 Text = $"Account No: {cust.AccountNo}  \nBalance: {cust.Balance}",
                                 Images = cardImages
                             }.ToAttachment()
                        );
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        await connector.Conversations.SendToConversationAsync(replyToConversation);
                        return Request.CreateResponse(HttpStatusCode.OK);
                        

                    }
                }


                /// --- Handle log in -------------------------
                if (userInput.ToLower().Equals("log in"))
                {
                    replyToUser = "Please enter your email.";
                    userData.SetProperty<bool>("userLogin", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    /// return 
                    Activity replyMessage2 = activity.CreateReply(replyToUser);
                    await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userLogin)
                {
                    int counter = loginInformation.Count;
                    if (counter == 0)
                    {
                        loginInformation.Add(userInput); // this is to store email. 
                        replyToUser = "Please enter your password.";
                        userData.SetProperty<List<string>>("loginInformation", loginInformation);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        /// return 
                        Activity replyMessage2 = activity.CreateReply(replyToUser);
                        await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    if (counter == 1)
                    {
                        loginInformation.Add(userInput); // this is to store password. 

                        string userEmail = loginInformation[0].ToString();
                        string userPassword = userInput; //loginInformation[1].ToString();

                        // Authenticate user 
                        bool validEmailAndPassword = await AzureManager.AzureManagerInstance.AuthenticateCustomer(userEmail, userPassword);
                        if (validEmailAndPassword) // authenticate successfully
                        {
                            // get user data; 
                            string accountNo = await AzureManager.AzureManagerInstance.getAccountNo(userEmail, userPassword);
                            Customer cust = await AzureManager.AzureManagerInstance.getCustomerDetails(accountNo);

                            userData.SetProperty<string>("userAccountNo", accountNo);
                            userData.SetProperty<Customer>("clientDetails", cust);
                            userData.SetProperty<bool>("userLoggedIn", true);
                            userData.SetProperty<bool>("userLogin", false);
                            userData.SetProperty<List<string>>("loginInformation", new List<string>());
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                            replyToUser = $"Log in successfully";

                            // ##  Account created page ##
                            Activity replyToConversation = activity.CreateReply("");
                            replyToConversation.Recipient = activity.From;
                            replyToConversation.Type = "message";
                            replyToConversation.Attachments = new List<Attachment>();

                            // CardImage
                            List<CardImage> cardImages = new List<CardImage>();
                            cardImages.Add(new CardImage(url: "http://stock.wikimini.org/w/images/9/95/Gnome-stock_person-avatar-profile.png"));


                            // Reply with Hero card
                            replyToConversation.Attachments.Add(
                                 new HeroCard
                                 {
                                     Title = "Welcome Back",
                                     Text = $"Account No: {cust.AccountNo}  \nBalance: ${cust.Balance}",
                                     Images = cardImages
                                 }.ToAttachment()
                            );
                            await connector.Conversations.SendToConversationAsync(replyToConversation);
                            return Request.CreateResponse(HttpStatusCode.OK);

                        }
                        else
                        {
                            replyToUser = "Invalid Email and Password.";
                            userData.SetProperty<bool>("userLoggedIn", false);
                            userData.SetProperty<bool>("userLogin", false);
                            userData.SetProperty<List<string>>("loginInformation", new List<string>());
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            /// return 
                            Activity replyMessage2 = activity.CreateReply(replyToUser);
                            await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                    }

                }




                //// --- User haven't log in ---------------------------

                if (!createAccount && !userLogin && !userLoggedIn && !userInput.ToLower().Equals("log in") && !userInput.ToLower().Equals("create account") && !userInput.ToLower().Equals("delete account"))
                {

                    /// Luis Intent 
                    LuisIntentObject luisIntentObject = await GetEntityFromLUIS(userInput);
                    switch (luisIntentObject.topScoringIntent.intent)
                    {
                        case "getHelp":
                            // ##  Introduction Page ##
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
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            await connector.Conversations.SendToConversationAsync(replyToConversation);
                            return Request.CreateResponse(HttpStatusCode.OK);

                        case "getCurrencyRate":
                            bool validCountryCode = CheckCountryCode(userInput);
                            switch (validCountryCode)
                            {
                                case true:
                                    replyToUser = await GetCurrencyRate(userInput);

                                    // return
                                    Activity replywithCurrencyMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replywithCurrencyMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);

                                default:
                                    replyToUser = "Do you want to check currency rate? If yes please enter 'convert country code to country code'.";
                                    // return
                                    Activity replywithNullIntentMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replywithNullIntentMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                            }

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
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            await connector.Conversations.SendToConversationAsync(replyToConversation2);
                            return Request.CreateResponse(HttpStatusCode.OK);
                    }


                }



                /// handle delete account. If user want to delete account, he or she must already log in first. 
                if (userLoggedIn && userInput.ToLower().Equals("delete account"))
                {
                    await AzureManager.AzureManagerInstance.DeleteCustomer(clientDetails);

                    userData.SetProperty<bool>("userLoggedIn", false);
                    userData.SetProperty<bool>("userLoggedIn", false);
                    userData.SetProperty<bool>("createAccount", false);
                    userData.SetProperty<bool>("deleteAccount", false);
                    userData.SetProperty<string>("userAccountNo", "");
                    userData.SetProperty<Customer>("clientDetails", new Customer());
                    userData.SetProperty<List<object>>("newUserInformation", new List<object>());
                    userData.SetProperty<List<string>>("loginInformation", new List<string>());
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    replyToUser = "Your account has been successfully deleted.";
                   
                    // return
                    Activity replyDelMessage = activity.CreateReply(replyToUser);
                    await connector.Conversations.ReplyToActivityAsync(replyDelMessage);
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
                            replyToUser = "Don't worry. I am here to help you";
                            Activity replyMessage2 = activity.CreateReply(replyToUser);
                            await connector.Conversations.ReplyToActivityAsync(replyMessage2);
                            return Request.CreateResponse(HttpStatusCode.OK);
                            
                        case "withdraw":
                            bool WithDrawSuccess = await Withdraw(clientDetails, userInput);
                            switch (WithDrawSuccess)
                            {
                                case true:
                                    userData.SetProperty<Customer>("clientDetails", clientDetails);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    replyToUser = $"Withdraw successfully. Your new balance is ${string.Format("{0:0.00}", clientDetails.Balance)}";

                                    // return
                                    Activity replywithdrawMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replywithdrawMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);

                                default:
                                    replyToUser = "Do you want to withdraw money? If yes please type 'withdraw amount'";

                                    // return
                                    Activity replyConfirmWithdrawMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replyConfirmWithdrawMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                            }
                          
                        case "deposit":
                            bool DepositSuccess = await Deposit(clientDetails, userInput);
                            switch (DepositSuccess)
                            {
                                case true:
                                    userData.SetProperty<Customer>("clientDetails", clientDetails);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    replyToUser = $"Deposited Successfully. Your new balance is ${string.Format("{0:0.00}", clientDetails.Balance)}";

                                    // return
                                    Activity replyDepositMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replyDepositMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                                    
                                default:
                                    replyToUser = "Do you want to deposit money? If yes please type 'deposit amount'";
                                    // return
                                    Activity replywithNullIntentMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replywithNullIntentMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                                    
                            }
                            
                        case "getCurrencyRate":
                            bool validCountryCode = CheckCountryCode(userInput);
                            switch (validCountryCode)
                            {
                                case true:
                                    replyToUser = await GetCurrencyRate(userInput);

                                    // return
                                    Activity replywithCurrencyMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replywithCurrencyMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);

                                default:
                                    replyToUser = "Do you want to check currency rate? If yes please enter 'convert country code to country code'.";
                                    // return
                                    Activity replywithNullIntentMessage = activity.CreateReply(replyToUser);
                                    await connector.Conversations.ReplyToActivityAsync(replywithNullIntentMessage);
                                    return Request.CreateResponse(HttpStatusCode.OK);
                            }

                        case "transfer":
                            replyToUser = "transfer";
                            break;

                        default:
                             await Conversation.SendAsync(activity, () => new TransactionDialog());
                             replyToUser = $"Sorry, i am not getting you...";
                             // return
                             Activity replyMessage3 = activity.CreateReply(replyToUser);
                             await connector.Conversations.ReplyToActivityAsync(replyMessage3);
                             return Request.CreateResponse(HttpStatusCode.OK);
                           
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
        /// check country codes
        /// </summary>
        /// <param name="userInput"></param>  // example convert nzd to aud 
        /// <returns></returns>
        private bool CheckCountryCode(string userInput)
        {

            List<string> codes = new List<string> { "AUD", "BGN", "BRL", "CAD", "CHF", "CNY", "CZK", "DKK", "EUR", "GBP", "HKD",
                                                    "HRK", "HUF", "IDR", "ILS", "INR", "JPY", "KRW", "MXN", "MYR", "NOK", "NZD",
                                                    "PHP", "PLN", "RON", "RUB", "SEK", "SGD", "THB", "TRY", "USD", "ZAR" };
            var result = userInput.Split(' ');
          
            if (result.Length == 4)
            {
                bool firstString = result[0].ToLower().Equals("convert");
                bool secondString = codes.Contains(result[1].ToUpper());
                bool thirdString = result[2].ToLower().Equals("to");
                bool fourthString = codes.Contains(result[3].ToUpper());
                return firstString & secondString & thirdString & fourthString;
            }

            return false;
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
            var result = userInput.Split(' ');

            string fromCurrency = result[1].ToUpper();
            string toCurrency = result[3].ToUpper();
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