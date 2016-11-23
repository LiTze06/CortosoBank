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
        bool askUserToLogin = false;
        bool createAccount;
        bool deleteAccount;
        string userAccountNo;
        string strEmailAndPassword;
        Customer clientDetails;
        List<object> newUserInformation;

        List<string> userInfo = new List<string> { "AccountNo", "Email", "Password", "FirstName", "LastName", "Age", "Balance" };

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
                string replyToUser = "";

                /// ---  Store client information ----------------
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                userLoggedIn = userData.GetProperty<bool>("userLoggedIn");
                askUserToLogin = userData.GetProperty<bool>("askUserToLogin");
                strEmailAndPassword = userData.GetProperty<string>("strEmailAndPassword") ?? "";
                userAccountNo = userData.GetProperty<string>("userAccountNo") ?? "";
                clientDetails = userData.GetProperty<Customer>("clientDetails") ?? new Customer();
                createAccount = userData.GetProperty<bool>("createAccount");
                deleteAccount = userData.GetProperty<bool>("deleteAccount");
                newUserInformation = userData.GetProperty<List<object>>("newUserInformation") ?? new List<object>();



                /// --- Create An account ---------------------
                if (userInput.ToLower().Equals("create account"))
                {
                    userInput = "Please enter .... ";
                    newUserInformation = new List<object>();
                    userData.SetProperty<bool>("createAccount", true);
                    createAccount = true; 
                    userData.SetProperty<bool>("askUserToLogin", true);
                    userData.SetProperty<List<object>>("newUserInformation", newUserInformation);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }



                /// --- Authenticate user and store information about the user ---------------------
                if (askUserToLogin & !userInput.ToLower().Equals("clear") ) // user logged in
                {
                    if (strEmailAndPassword == "") // User haven't provide email and password
                    {
                        bool validEmailAndPassword = await CheckEmailAndPassword(userInput);
                        
                        if (validEmailAndPassword) // authenticate successfully
                        {
                            // get user data; 
                            string[] EmailandPassword = userInput.Split(',').Select(sValue => sValue.Trim()).ToArray();
                            string accountNo = await AzureManager.AzureManagerInstance.getAccountNo(EmailandPassword[0], EmailandPassword[1]);
                            Customer cust = await AzureManager.AzureManagerInstance.getCustomerDetails(accountNo);

                            replyToUser = $"Login in successfully! Hi {cust.FirstName}!";

                            userData.SetProperty<string>("strEmailAndPassword", userInput);
                            userData.SetProperty<string>("userAccountNo", accountNo);
                            userData.SetProperty<Customer>("clientDetails", cust);
                            userData.SetProperty<bool>("userLoggedIn", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }
                        else
                        {
                            /// --- Create An account ---------------------
                            if (userInput.ToLower().Equals("create account"))
                            {
                                
                                newUserInformation = new List<object>();
                                userData.SetProperty<bool>("createAccount", true);
                                userData.SetProperty<List<object>>("newUserInformation", newUserInformation);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            }


                            if (createAccount)
                            {
                                // Get new user's account information
                                int counter = newUserInformation.Count + 1;
                                if (counter < 7)
                                {
                                    replyToUser = $"({counter}/6): Please enter your {userInfo[counter]}";
                                    newUserInformation.Add(userInput);
                                    userData.SetProperty<List<object>>("newUserInformation", newUserInformation);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                }
                                else
                                {
                                    // User's all information are there. Now is to create account ! 
                                    newUserInformation.Add(userInput);
                                    userData.SetProperty<List<object>>("newUserInformation", newUserInformation);

                                    // Get Unique AccountNo 
                                    string newAccountNo = await AzureManager.AzureManagerInstance.getUniqueAccountNo();

                                    Customer cust = new Customer();
                                    cust.AccountNo = newAccountNo;
                                    cust.Email = newUserInformation[1].ToString();
                                    cust.Password = newUserInformation[2].ToString();
                                    cust.FirstName = newUserInformation[3].ToString();
                                    cust.LastName = newUserInformation[4].ToString();
                                    cust.Age = Int32.Parse(newUserInformation[5].ToString());
                                    cust.Balance = Double.Parse(newUserInformation[6].ToString());

                                    replyToUser = $" Welcome to Cortoso Bank! Your account has been successfully created. Your accountNo is {newAccountNo} with the balance of ${cust.Balance}.";
                                    string EmailAndAddress = $"{cust.Email},{cust.Password}";

                                    await AzureManager.AzureManagerInstance.AddCustomer(cust);

                                    createAccount = false;
                                    userData.SetProperty<string>("strEmailAndPassword", EmailAndAddress);
                                    userData.SetProperty<bool>("userLoggedIn", true);
                                    userData.SetProperty<bool>("createAccount", false);
                                    userData.SetProperty<string>("userAccountNo", newAccountNo);
                                    userData.SetProperty<Customer>("clientDetails", cust);
                                    userData.SetProperty<List<object>>("newUserInformation", new List<object>());
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                }
                          
                            }
                            else
                            {
                                replyToUser = $"Invalid Email and Password. Please try again. For more information please type 'help'. To create an account, type 'create account'.";
                                userData.SetProperty<bool>("askUserToLogin", false);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            }
                          
                        }
                    }
                    /*
                    else // User provided email and password
                    {
                        // Timeout !! 
                        replyToUser = $"You entered: {userInput}";
                    }
                    */
                }
                else // user haven't logged in 
                {
                    // ask user to log in 
                    replyToUser = $"Hello, I am Cortoso Bank Bot. Please log in using your email and password separate by comma. If you do not have an account, please type 'create account'.";
                    userData.SetProperty<bool>("askUserToLogin", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }


                /// 
                /// -- User has log in successfully ! Now, what the user want -------------
                /// 
                if (userAccountNo != "" &  !userInput.ToLower().Equals("clear"))
                {
                    /// Luis Intent 
                    LuisIntentObject luisIntentObject = await GetEntityFromLUIS(userInput);
                    switch (luisIntentObject.topScoringIntent.intent)
                    {
                        case "viewbalance":
                            double Balance  = await ViewBalance(userAccountNo);
                            replyToUser = $"Your balance is ${string.Format("{0:0.00}", Balance)}.";
                            break;
                        case "getHelp":
                            await Conversation.SendAsync(activity, () => new HelpDialog());
                            break;
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
                                replyToUser = "Sorry, i am not getting you. Please type 'help' for more information.";
                            }
                            break;
                    }
                   
                }



                /// handle delete account. If user want to delete account, he or she must already log in first. 
                if (userInput.ToLower().Equals("delete account"))
                {
                    await AzureManager.AzureManagerInstance.DeleteCustomer(clientDetails);

                    userData.SetProperty<string>("strEmailAndPassword", "");
                    userData.SetProperty<bool>("userLoggedIn", false);
                    userData.SetProperty<string>("userAccountNo", "");
                    userData.SetProperty<Customer>("clientDetails", new Customer());

                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    replyToUser = "Your account has been successfully deleted.";
                }


                /// --- Help -----------------------
                if (userAccountNo == "" & userInput.ToLower().Equals("help"))
                {
                    replyToUser = "How can i help you?";
                    await Conversation.SendAsync(activity, () => new HelpDialog());
                }


                /// --- Clear state  ---------------------
                if (userInput.ToLower().Equals("clear"))
                {
                    replyToUser = "Your state has been cleared. ";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                }



                // Create a reply message
                Activity replyMessage = activity.CreateReply(replyToUser);
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
        /// To validate authorisation
        /// </summary>
        /// <param name="email,address"></param>
        /// <returns></returns>
        private static async Task<bool> CheckEmailAndPassword(string userInput)
        {
            string[] EmailandPassword = userInput.Split(',').Select(sValue => sValue.Trim()).ToArray();
            if (EmailandPassword.Length == 2)
            {
                bool authenticateUser = await AzureManager.AzureManagerInstance.AuthenticateCustomer(EmailandPassword[0], EmailandPassword[1]);
                if (authenticateUser)
                {
                    return true;
                }
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