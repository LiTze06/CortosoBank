using CortosoBank.Models;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CortosoBank
{
    public class AzureManager
    {

        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<Customer> customerTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("http://mycortosobank.azurewebsites.net");
            this.customerTable = this.client.GetTable<Customer>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task AddCustomer(Customer customer)
        {
            await this.customerTable.InsertAsync(customer);
        }

        public async Task<List<Customer>> GetCustomerList()
        {
            return await this.customerTable.ToListAsync();
        }

        public async Task UpdateCustomerDetail(Customer customer)
        {
            await this.customerTable.UpdateAsync(customer);
        }

        public async Task DeleteCustomerDetail(Customer customer)
        {
            await this.customerTable.DeleteAsync(customer);
        }

        // increase saving 
        public async Task<double> DepositMoney(Customer customer, double newBalance)
        {
            customer.Balance = customer.Balance + newBalance;
            await this.UpdateCustomerDetail(customer);
            return customer.Balance;
        }

        // decrease saving 
        public async Task<double> WithdrawMoney(Customer customer, double newBalance)
        {
            customer.Balance = customer.Balance - newBalance;
            await this.UpdateCustomerDetail(customer);
            return customer.Balance;
        }

     
        public async Task<double> getBalance(string accountNo)
        {
            List<Customer> customerList = await this.GetCustomerList();
            for (int i = 0; i < customerList.Count(); i++)
            {
                string AccountNo = customerList[i].AccountNo;


                if (AccountNo.Equals(accountNo))
                {
                    return customerList[i].Balance;
                }
            }
            return -1;  
        }
        
       

        public async Task<Customer> getCustomerDetails(string accountNo)
        {
          
            List<Customer> customerList = await this.GetCustomerList();
            for (int i = 0; i < customerList.Count(); i++)
            {
                string AccountNo = customerList[i].AccountNo;
                
                if (AccountNo.Equals(accountNo))
                {
                    return customerList[i];
                }
            }
            return new Customer();
        }


        public async Task<bool> AuthenticateCustomer(string email, string password)
        {
            List<Customer> customerList = await this.GetCustomerList();

            for (int i = 0; i < customerList.Count(); i++)
            {
                string Email = customerList[i].Email;
                string Password = customerList[i].Password;

                if (Email.Equals(email) & Password.Equals(Password))
                {
                    return true;
                }
            }
            return false; 
        }

        public async Task<string> getAccountNo(string email, string password)
        {
            List<Customer> customerList = await this.GetCustomerList();

            for (int i = 0; i < customerList.Count(); i++)
            {
                string Email = customerList[i].Email;
                string Password = customerList[i].Password;
                string AccountNo = customerList[i].AccountNo;

                if (Email.Equals(email) & Password.Equals(Password))
                {
                    return AccountNo;
                }
            }
            return "";
        }


      
    }
}