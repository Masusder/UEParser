using System.Collections.Generic;

namespace UEParser.APIComposers;

public class Currency
{
    public required string IconPath { get; set; }
    public required string BackgroundPath { get; set; }
}

public static class Currencies
{

    public static Dictionary<string, Currency> CurrencyList = new Dictionary<string, Currency>
    {
        {
            "Cells", new Currency
            {
                IconPath = "/images/Currency/AuricCells_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_AuricCells.png",
            }
        },
        {
            "Shards", new Currency
            {
                IconPath = "/images/Currency/Shards_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_Shards.png",
            }
        },
        {
            "Bloodpoints", new Currency
            {
                IconPath = "/images/Currency/Bloodpoints_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_Bloodpoints.png",
            }
        },
        {
            "BonusBloodpoints", new Currency
            {
                IconPath = "/images/Currency/Bloodpoints_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_Bloodpoints.png",
            }
        },
        {
            "WinterEventCurrency", new Currency
            {
                IconPath = "/images/Currency/WinterEventCurrency_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_WinterEventCurrency.png",
            }
        },
        {
            "AnniversaryEventCurrency", new Currency
            {
                IconPath = "/images/Currency/AnniversaryEventCurrency_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_AnniversaryEventCurrency.png",
            }
        },
        {
            "HalloweenEventCurrency", new Currency
            {
                IconPath = "/images/Currency/HalloweenEventCurrency_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_HalloweenEventCurrency.png",
            }
        },
        {
            "SpringEventCurrency", new Currency
            {
                IconPath = "/images/Currency/SpringEventCurrency_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_SpringEventCurrency.png",
            }
        },
        {
            "RiftFragments", new Currency
            {
                IconPath = "/images/Currency/RiftFragmentsIcon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_WinterEventCurrency.png"
            }
        },
        {
            "PutridSerum", new Currency
            {
                IconPath = "/images/Currency/PutridSerum_Icon.png",
                BackgroundPath = "/images/Currency/CurrencyBackground_HalloweenEventCurrency.png"
            }
        }
    };
}