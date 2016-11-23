using CortosoBank.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CortosoBank
{
    public class ExchangeRate
    {
        public const string AUD = "AUD";
        public const string BGN = "BGN";
        public const string BRL = "BRL";
        public const string CAD = "CAD";
        public const string CHF = "CHF";
        public const string CNY = "CNY";
        public const string CZK = "CZK";
        public const string DKK = "DKK";
        public const string EUR = "EUR";
        public const string GBP = "GBP";
        public const string HKD = "HKD";
        public const string HRK = "HRK";
        public const string HUF = "HUF";
        public const string IDR = "IDR";
        public const string ILS = "ILS";
        public const string INR = "INR";
        public const string JPY = "JPY";
        public const string KRW = "KRW";
        public const string MXN = "MXN";
        public const string MYR = "MYR";
        public const string NOK = "NOK";
        public const string NZD = "NZD";
        public const string PHP = "PHP";
        public const string PLN = "PLN";
        public const string RON = "RON";
        public const string RUB = "RUB";
        public const string SEK = "SEK";
        public const string SGD = "SGD";
        public const string THB = "THB";
        public const string TRY = "TRY";
        public const string USD = "USD";
        public const string ZAR = "ZAR";

        public string fromCurrency { get; private set; }
        public string toCurrency { get; private set; }
        public Rates currencyRates { get; set; }
        public double rates = 1.0;

        public double Convert(double amount)
        {
            return amount * rates;
        }


        public ExchangeRate(string from, string to, CurrencyObject currencyObject)
        {
            this.fromCurrency = from;
            this.toCurrency = to;
            this.currencyRates = currencyObject.rates;
        }

        public double getCurrencyRate()
        {
            if (toCurrency.Equals("AUD"))
            {
                rates = currencyRates.AUD;
            }
            else if (toCurrency.Equals("BGN"))
            {
                rates = currencyRates.BGN;
            }
            else if (toCurrency.Equals("BRL"))
            {
                rates = currencyRates.BRL;
            }
            else if (toCurrency.Equals("CAD"))
            {
                rates = currencyRates.CAD;
            }
            else if (toCurrency.Equals("CHF"))
            {
                rates = currencyRates.CHF;
            }
            else if (toCurrency.Equals("CNY"))
            {
                rates = currencyRates.CNY;
            }
            else if (toCurrency.Equals("CZK"))
            {
                rates = currencyRates.CZK;
            }
            else if (toCurrency.Equals("DKK"))
            {
                rates = currencyRates.DKK;
            }
            else if (toCurrency.Equals("GBP"))
            {
                rates = currencyRates.GBP;
            }
            else if (toCurrency.Equals("EUR"))
            {
                rates = currencyRates.EUR;
            }
            else if (toCurrency.Equals("HKD"))
            {
                rates = currencyRates.HKD;
            }
            else if (toCurrency.Equals("HRK"))
            {
                rates = currencyRates.HRK;
            }
            else if (toCurrency.Equals("HUF"))
            {
                rates = currencyRates.HUF;
            }
            else if (toCurrency.Equals("IDR"))
            {
                rates = currencyRates.IDR;
            }
            else if (toCurrency.Equals("ILS"))
            {
                rates = currencyRates.ILS;
            }
            else if (toCurrency.Equals("INR"))
            {
                rates = currencyRates.INR;
            }
            else if (toCurrency.Equals("JPY"))
            {
                rates = currencyRates.JPY;
            }
            else if (toCurrency.Equals("KRW"))
            {
                rates = currencyRates.KRW;
            }
            else if (toCurrency.Equals("MXN"))
            {
                rates = currencyRates.MXN;
            }
            else if (toCurrency.Equals("MYR"))
            {
                rates = currencyRates.MYR;
            }
            else if (toCurrency.Equals("NOK"))
            {
                rates = currencyRates.NOK;
            }
            else if (toCurrency.Equals("NZD"))
            {
                rates = currencyRates.NZD;
            }
            else if (toCurrency.Equals("PHP"))
            {
                rates = currencyRates.PHP;
            }
            else if (toCurrency.Equals("PLN"))
            {
                rates = currencyRates.PLN;
            }
            else if (toCurrency.Equals("RON"))
            {
                rates = currencyRates.RON;
            }
            else if (toCurrency.Equals("RUB"))
            {
                rates = currencyRates.RUB;
            }
            else if (toCurrency.Equals("SEK"))
            {
                rates = currencyRates.SEK;
            }
            else if (toCurrency.Equals("SGD"))
            {
                rates = currencyRates.SGD;
            }
            else if (toCurrency.Equals("THB"))
            {
                rates = currencyRates.THB;
            }
            else if (toCurrency.Equals("TRY"))
            {
                rates = currencyRates.TRY;
            }
            else if (toCurrency.Equals("USD"))
            {
                rates = currencyRates.USD;
            }
            else if (toCurrency.Equals("ZAR"))
            {
                rates = currencyRates.ZAR;
            }



            return rates;
        }

    }
}