using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UEParser.Models;

public class Configuration
{
    public CoreConfig Core { get; set; }
    public NeteaseConfig Netease { get; set; }
    public GlobalConfig Global { get; set; }
    public SensitiveConfig Sensitive { get; set; }

    public Configuration()
    {
        Core = new CoreConfig();
        Netease = new NeteaseConfig();
        Global = new GlobalConfig();
        Sensitive = new SensitiveConfig();
    }
}

// Global config
public class GlobalConfig
{
    public Dictionary<string, string> BranchRoots { get; set; }
    public string BlenderPath { get; set; }
    public bool UpdateAPIDuringInitialization { get; set; }

    public GlobalConfig()
    {
        BranchRoots = [];
        BlenderPath = "";
        UpdateAPIDuringInitialization = false;
    }
}

// Config for core
public class CoreConfig
{
    public string BuildVersionNumber { get; set; }
    public string PathToGameDirectory { get; set; }
    public string MappingsPath { get; set; }
    public string AesKey { get; set; }
    public HashSet<string> TomesList { get; set; }
    public HashSet<string> EventTomesList { get; set; }
    public VersionData VersionData { get; set; }
    public KrakenApiConfig ApiConfig { get; set; }

    public CoreConfig()
    {
        BuildVersionNumber = "";
        PathToGameDirectory = "";
        MappingsPath = "";
        AesKey = "0x22b1639b548124925cf7b9cbaa09f9ac295fcf0324586d6b37ee1d42670b39b3";

        var defaultTomes = new HashSet<string>
        {
            "Tome01", "Tome02", "Tome03", "Tome04", "Tome05", "Tome06", "Tome07",
            "Tome08", "Tome09", "Tome10", "Tome11", "Tome12", "Tome13", "Tome14",
            "Tome15", "Tome16", "Tome17", "Tome18", "Tome19", "Tome20"
        };

        var defaultEventTomes = new HashSet<string>
        {
            "Halloween2021", "Halloween2022", "Anniversary2022", "Anniversary2023",
            "Winter2022", "Summer2023", "Halloween2023", "Winter2023",
            "DreadByDaylightZodiac", "Spring2024", "ChocolateBoxV1", "Anniversary2024", "CalamariV1"
        };

        TomesList = defaultTomes;
        EventTomesList = defaultEventTomes;
        VersionData = new VersionData();
        ApiConfig = new KrakenApiConfig();
    }
}

// Version data
public class VersionData
{
    public string? LatestVersionHeader { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Branch Branch { get; set; }
    public string? CompareVersionHeader { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Branch CompareBranch { get; set; }

    public VersionData()
    {
        LatestVersionHeader = "";
        CompareVersionHeader = "";
        Branch = Branch.live;
        CompareBranch = Branch.live;
    }
}

// Kraken (API/CDN)
public class KrakenApiConfig
{
    public string LatestVersion { get; set; }
    public string? CustomVersion { get; set; }
    public string ApiBaseUrl { get; set; }
    public string SteamApiBaseUrl { get; set; }
    public string CdnBaseUrl { get; set; }
    public string CdnContentSegment { get; set; }
    public Dictionary<string, string> DynamicCdnEndpoints { get; set; }
    public Dictionary<string, string> CdnEndpoints { get; set; }
    public Dictionary<string, string> ApiEndpoints { get; set; }
    public Dictionary<string, string> S3AccessKeys { get; set; }

    public KrakenApiConfig()
    {
        LatestVersion = "";
        CustomVersion = null;
        ApiBaseUrl = "https://latest.{0}.bhvrdbd.com/api/v1";
        SteamApiBaseUrl = "https://steam.{0}.bhvrdbd.com/api/v1";
        CdnBaseUrl = "https://cdn.{0}.bhvrdbd.com";
        CdnContentSegment = "/clientData/{0}/content/";
        DynamicCdnEndpoints = new()
        {
            { "Tomes", "/archiveQuests/{0}.json" },
            { "Rifts", "/archiveRewardGrid/{0}.json" }
        };
        CdnEndpoints = new()
        {
            { "catalog", "/catalog.json" },
            { "crossPromoCampaigns", "/crossPromoCampaigns.json" },
            { "challengeRewardTrackers", "/challengeRewardTrackers.json" },
            { "challengeSets", "/challengeSets.json" },
            { "specialEventsContent", "/specialEventsContent.json" },
            { "dynamicContent", "/dynamicContent/DynamicContent.json" },
            { "archiveRewardData", "/archiveRewardData/content.json" },
            { "collections", "/collections.json" },
            { "specialEventsLoadingText", "/loadingText/specialEventsLoadingText_en.json" }
        };
        ApiEndpoints = new()
        {
            { "contentVersion", "/utils/contentVersion/version" }
        };
        // Gotta Catch ’Em All
        S3AccessKeys = new()
        {
            // Start of V2 encryption algorithm
            { "4.6.0", "uRqLnp6p9WUrTJ6nNXlv7z9VZRjbXvRFKEcF/spEn9k=" },
            { "4.7.0", "DJ1LTHLgxRNq7v7fsyG3AQONlsdN49gJ+oY9UuVCSzQ=" },
            { "5.0.0", "CADqND0WPwViwTPzhAiOjR/IrB5TCFInww+k1cmUg70=" },
            { "5.1.0", "GgkY5gFWXzqqxaqUFGb2x+CzdGuJ00nJ2XwV+AoBwuc=" },
            { "5.2.0", "gqONp7FrUqdbp3hS/iMmhphUQ5yPH8eKlkDQk+2QVkI=" },
            { "5.3.0", "vNYfdH/OVNau1dUy/JOIMMkI+gPxnquB69nedKGBBvk=" },
            // End of V2 encryption algorithm
            // Start of V3 encryption algorithm
            { "5.3.0_cert", "6HxjMJLRW4DXPqOj9y6af2zO95HkqYhH6uTxipELMZw=" },
            { "5.3.0_live", "sh+W8ya0xlYBYQoqhgMxJ+TdqlETZBbcUdVaRNq2l+A=" },
            { "5.3.0_ptb", "xu1pyspLvsA7BkL71zUNMq4gNCaGoGRopv9+68HQ3R0=" },
            { "5.3.0_qa", "T9ipuFpNYfH9eK2W+qKngjXlEh/rNxAgfKluhrQ8pHY=" },
            { "5.3.0_stage", "7Xy1EhefGI30O5y62QmS23VzfHoZEfd2cPTqAMk81wc=" },
            { "5.4.0", "8vHRSte+JkJnwbErHslCML7UXRawN8hx9FINn/HpuLM=" },
            { "5.4.0_cert", "s813m5+CZAWo7HLU7oAgfvo4ZMp1dgklOcWYL0z+LAk=" },
            { "5.4.0_dev", "d0Q676qczVqkup5zSSCFlyoD7mtDtRVxzVT7YLc8+7U=" },
            { "5.4.0_live", "SxBJ00x1oWSTkah/ytJK890YrHwFS5K/6KQofRskCLw=" },
            { "5.4.0_ptb", "KUfgt6VBCgoZgIAHMa4oqASaaLglS1jC1DmrMDSQ28g=" },
            { "5.4.0_qa", "7NDp19kO4GgTqte03hsE1DWGcmQ3aL+14mO6EfMbI8g=" },
            { "5.4.0_stage", "3qb6mxIxMrJy6PxDkXMKsyZgoZexa68eplwP/ld5Fs0=" },
            { "5.4.0_uat", "Np9TLtCCtQIVMoXz3q+ckOt2GiujuP3dHD1yj4YGC8w=" },
            { "5.5.0_cert", "qPaBUUxFyyrFMuFehEsYl48zJux7agrFRPG1Mn04cXo=" },
            { "5.5.0_live", "oTiFYoEBHt3hC+VNWoH5UhCiYI9VbSom+PMQ7g8FT0c=" },
            { "5.5.0_ptb", "8OM9ykO6lGSRtljtTbnJD8KLhe59IK82jl3IUBKO9rA=" },
            { "5.5.0_qa", "BknFgXjvKjDr7w/YlLXMYprfghbIlVJ6GQzS0v+0VoY=" },
            { "5.5.0_stage", "2XQBGJFhhD5fLbyphPZeqDwwIiTZp3m4pgOWBxJB2gU=" },
            { "5.6.0", "D/9ER7cAor0icE+1zyeQVXw9C1psJzrPh2/oS+tJgs0=" },
            { "5.6.0_cert", "H9P+XNQcvPDsPbfPzlEQAa9wwLLELN5YCfEMMQaTlL8=" },
            { "5.6.0_live", "5KW8urNuyztcKyqF1h/eJ0v6/lCcTjPW/tTyD+AHw8M=" },
            { "5.6.0_ptb", "DmrJVDG631UkxDDVM7awzOG8ErlbtXLL9QS0ySPgJe0=" },
            { "5.6.0_qa", "HRoXOqvL+41vE/+uNkYmdXWcovszJ2RlscB/4Fb1XIw=" },
            { "5.6.0_stage", "fowjOfz5iAPj9LGTFWeFvJrP3CYqxSkqFgvK8Aii/58=" },
            { "5.7.0", "+Xz9ctno/PGWBPa6II2+8YTKpK1MlLs/EbuRBZO+ag0=" }, // Global keys have been deprecated as of 5.7.0
            { "5.7.0_cert", "t7ChbXsRRHp8zmAezKQOhje2Mzer/ZlBdMUXxrQtGp4=" },
            { "5.7.0_live", "i/PvMOjjZvG23HS+kocrzGsOBZk8M7ZipTENTX0dvNM=" },
            { "5.7.0_ptb", "l15ppTB/NkKXnvT25wjwOGglWEFd4pYfXcfe7zwlYY0=" },
            { "5.7.0_qa", "+xXjYE55EfRrrupPp4kCwp15XhrZdvlXTNvhwzWNWt8=" },
            { "5.7.0_stage", "nyIY3cPt2tIJTzYIwApeYTFdhlCIlQBNYsf/A8p5Pys=" },
            { "6.0.0_cert", "TgvjLDrOh+ioX6PWDYUawtVaaUljKSpVtTb8MjFhxnc=" },
            { "6.0.0_live", "mlySUt8ePI7qofwOUG3W/9BMcrvxq/w/AJCffC+uJaw=" },
            { "6.0.0_ptb", "GGsLKaEsVsV4mS8i+a3JYNT8TawepHeIc5weB+7dC4E=" },
            { "6.0.0_qa", "Sr3q2cSIEUu2fOnL66l5EHLCAg9PaTnkD/2p5mMSey0=" },
            { "6.0.0_stage", "2lKpO2OKcz9wHEEWW23p2Mtu3D/7u/OSeD6cIF5Czbk=" },
            { "6.1.0_cert", "RXhbjNZ6d4baqayDRd0kxQqLj4fMp8Q95LAc8NCyD/c=" },
            { "6.1.0_live", "ls2KAKwwnLfZ0+y/1mgtRDOCEQiZlE1wEz8HFxqcars=" },
            { "6.1.0_ptb", "WWwM6gKhhOm6LMaIzG6BvkJXRuJbAR1P0UGcsxQwOug=" },
            { "6.1.0_qa", "T2EK3LZrNgMj4n+YzMirB26tHu8MrRWpXpIxd3mQP2k=" },
            { "6.1.0_stage", "buWSwzZB+sWGfmoRFgoIKRZenDGURNi+qvzamRE8zCc=" },
            { "6.2.0_cert", "YhHvDj8xig9l+nNeCsK2Atzctb3QrGChP3WQP6XcKbU=" },
            { "6.2.0_live", "Qh6x2oUwbZviGgR7PY7++SEcn9aSXJ6bf2z4+uXzgIs=" },
            { "6.2.0_ptb", "2JUaH2YO5cUc5I4TquSQx+npcmwc0HKcS0QvoSIdgsY=" },
            { "6.2.0_qa", "bsWeKVMS5BjvrCP0vhmKWdGc6D7Vv9g4yrhIGxKl7ts=" },
            { "6.2.0_stage", "CsRcwfFRfyz/Wr2b9I13F8IcDjgl9b3JvfZa4AHwRwk=" },
            { "6.3.0_cert", "e1/OGBic1fVqkQshsZGgqFkUHCf9ggbDrsdiT8ZFPUw=" },
            { "6.3.0_live", "HzYnisndfs2ZJohA5LjrinEUqmcOq5OIs8msfEzSG7M=" },
            { "6.3.0_ptb", "GuP9FpMO3tcq7mu/+1y8OVpTDqxynwBCy2OeWbKw5mM=" },
            { "6.3.0_qa", "kznBHc6ygsKU8NJP5oxAPkn076QpVd/d0jBC4K9no9Y=" },
            { "6.3.0_stage", "9CfFDvyYzz4xOhXzP6ufrva3qM5SXWGwRhS8iQ9e8QM=" },
            { "6.4.0_cert", "ea9v+N6EK1AHZLcJzjnUlzezDhmU7L3bMG4ml5DONJs=" },
            { "6.4.0_live", "KQNDvFhylo7Pg/KrP7H/F3+u31wCewXvTB9H/S/Zlh0=" },
            { "6.4.0_ptb", "TGOZ2IUMnx9lhEbYPBfa7MMcuqCHYx3IX2tey3/1X+Y=" },
            { "6.4.0_qa", "meZAf747WuKFWobmdWNXU7/iESbM78ovIWpk67HoLrM=" },
            { "6.4.0_stage", "8fdNWKflmU6soTwvK6V2YOj4WyQo0WCfEOmhAIMDpMI=" },
            { "6.5.0_cert", "lDMoZmg86J/JJ3tnW3rcDYpeZ2meLzlaCWUUSGHT8lo=" },
            { "6.5.0_live", "2MKDqFlk0pS9JXP8KbRYGFIUu7oY9ZpJGUIpbxI2T28=" },
            { "6.5.0_ptb", "J2sMGSFPSl3hvKI5RZibSVfb1NcOGCQ4nSRK1QDwPAc=" },
            { "6.5.0_qa", "KUa5irue4xzC2tFJDWfJ11DJL6FMFYHuJbpRmM+HteA=" },
            { "6.5.0_stage", "U62EwEodvZMTqwj2dPyuLwX/nznIPi5XlsVdwprDWsU=" },
            { "6.6.0_cert", "kZLRPQKW6NdZYULTUvCVXJmf2PbohJamENiZpBUiZUM=" },
            { "6.6.0_live", "c6KYurOiikoX5kir08IRt1Rpuz5IIey7VAmTlTsG6hY=" },
            { "6.6.0_ptb", "uw3ae6mCueyhhdhiUajkrGCFhVreB81evnpfKTlrVNU=" },
            { "6.6.0_qa", "PpRBHYZUTCZj8o+VbUM8mMkyYLioatv9wRnbdcJ4DIM=" },
            { "6.6.0_stage", "LdE6NNAVuyfvZnMYGcymCqGdZCLADAjc3/05+F06hFI=" },
            { "6.7.0_cert", "mvofsGrnsYkm+F79gwuMQiRT7uPz7LJo+IJNdZG3HMk=" },
            { "6.7.0_dev", "8HDrCi+TbetmH00aCcGGYqhw3TDjyVLJyfIyZeqescA=" },
            { "6.7.0_live", "G2y1Ixl5TedkbOr2VOQkgc5PZ4w2dWq+V/+5jS6zH0Q=" },
            { "6.7.0_local", "tDPD+nyoDG5Ld+8aMVI+dx3+odEzg9ozOOLc6ScT/kw=" },
            { "6.7.0_ptb", "tc+8k2SCG2HZxdfgquAdgVOKnCLMTHp0ZtpTOP17cPI=" },
            { "6.7.0_qa", "ApfqQz1BBLRFn6ks/oLA7bmYzYTnHwQq1iN922UKFJM=" },
            { "6.7.0_stage", "yLt0sEFoNqtAb5zmrdM3nZO2L2xsIYjBxI8T1XU9rSo=" },
            { "6.7.0_uat", "UuMAhf0erIIwE48cBm9hIIBtNJYavUcRuuOT1e7zqxM=" },
            { "7.0.0_cert", "6xQ+zMHMQoz45h8PjTYgFtwmaWNWSiqIgzjS1+/eODo=" },
            { "7.0.0_dev", "agO9dtwCtxWEBU26Qks2UHWIa3sVCXfkTg9nYgaQDxE=" },
            { "7.0.0_live", "/PVUcFp33ObpOXSic5bVpU+eY4zAQDhEKA/FxP3i5kA=" },
            { "7.0.0_local", "+BXxCxjR7m/Ajn9zgKv0xTaJGbm4F7fbkxHeIerOLVE=" },
            { "7.0.0_ptb", "ccsIaxxPzly6DVixEsPC0qX1I8f/HputfM423IRQGSk=" },
            { "7.0.0_qa", "+Co2NgSOqrodsO+lG0rUQHIM8z3AZJr9q+MxtL9RxiU=" },
            { "7.0.0_stage", "Q/UOB7BQC6Gcp9hsT1fCBaza1OuqqpbACbo3Jv1hpig=" },
            { "7.0.0_uat", "0e1UXfVlc2CkkPKAppZGRtY30OFTMx4XkJh55HSonX4=" },
            { "7.1.0_cert", "6Sk+yZgf+93tzXrUWNwLBUSdOUKGkMC1ZB7r2VXbISI=" },
            { "7.1.0_dev", "+MZ+yNgP9GmIREFHxCH+OhpfnABg+zwZjG7bo62NJgU=" },
            { "7.1.0_live", "TawMyQRJZwA6FRT2NzjQGPMslXb7+eUr27VBqG/wUX8=" },
            { "7.1.0_local", "6JRTZ9N5NZt8Wp5Ey74IYWiWPMxKDCl7Aa5VjWDlOkk=" },
            { "7.1.0_ptb", "VusVbSSbqSapPA8YMWGr8jsk6bIMOh8vsxu4E8LT98c=" },
            { "7.1.0_qa", "JFL1/n0nAL4LKFSlbEWtwguLSL7Jxv/ilu5kmB8ZH4Y=" },
            { "7.1.0_stage", "i0vwMnP2OSnKpBv84Wwvqmxq6ak4Cm/BeQG0fdUtzAA=" },
            { "7.1.0_uat", "c+Cdvyy3tvFUEtqI/9rG+Pa+fQ4d3AY7eZbSeDNap3U=" },
            { "7.2.0_cert", "447mhwrWuSruy+zEU4QNYM0mdwcz+GYRUOfWKhig+G0=" },
            { "7.2.0_dev", "RfzhX9inmSSeGntRV5BWGNG0qBeaShljkV5uxMDa0TE=" },
            { "7.2.0_live", "j9IdzvRXX3VC4jKMkdumpcXQLKpDbKD6SLjTN3fAOYQ=" },
            { "7.2.0_local", "g6KwV2mE4gUWZbbb3dt83+ualOGuLsFeA3TRkSRHfQg=" },
            { "7.2.0_ptb", "Cafo/GRR0NxSlgPLsPkzcmgKq6A3NFs2mgSSE8Q0zkI=" },
            { "7.2.0_qa", "D20EWX/zr+R8pHk1APdZIRXl2oBIqYMcLe4vkX7DvKQ=" },
            { "7.2.0_stage", "1wEkUmZOy8uQ+lFswTxIomu/Jl+A6WfxuwVviNnpooE=" },
            { "7.2.0_uat", "G4SDw8T7G8KZ9VHvTxcgl5Tt56+qGOh3ITn+4a/JEOE=" },
            { "7.3.0_cert", "mH3kdghw4yP3ltFEuGB1kmo5GmpJ4jvK5gdrVRVkSjk=" },
            { "7.3.0_dev", "gL8fv1k9At55NdqQVJTwP2b6DFgzH5LUAh32nzqRzf0=" },
            { "7.3.0_live", "OB5TjLTk9uz1V1duN4sAulSWZDqJDv0EhDvtPFq5lc8=" },
            { "7.3.0_local", "tdj4Q5nGL+dHa/jeE6ZafsarL02+cA0XY8F8HyO1Idc=" },
            { "7.3.0_ptb", "6DTZg9UKKc7STTY2piHfh0znNsOUx+Vs2bcrACj9X3w=" },
            { "7.3.0_qa", "xOoHX9l3nJAZPxVhH8rKp1/1/UR3X9IEedlabMEpmx4=" },
            { "7.3.0_stage", "Ke15x60ulGZBwikhEcKRO4hdYHshsPvlSWmOhhOWLMk=" },
            { "7.3.0_uat", "PmCJppTRYUT9qAlwvaJfiCn6ffEzBxxIDv3RIuNt/vE=" },
            { "7.3.3_cert", "ih++PDm2ua7oGdBz7mHKon3ayUVQaCVWaGatjPaUvCg=" },
            { "7.3.3_dev", "efVUWHxegkpE+Gxkgd+W29v6GJfajzHm+0HlR/1ayoQ=" },
            { "7.3.3_live", "ekWpqSaos7lfTcX55a4duRuGgdCLwnklqGuNdgIjbl0=" },
            { "7.3.3_local", "9+5R7hRBjsm7JokldDreVqAkvCaPJ63c5QvUEIO3VCU=" },
            { "7.3.3_ptb", "MHatdnGeQjPpeO8kF1eMD2AoY3gcSezLeKG3aW00cSU=" },
            { "7.3.3_qa", "mClJK+Z0GnN2GBmGfnRVCY9Rw1YmIYHwemwGAEZW6kk=" },
            { "7.3.3_stage", "vYeC44KAF6X4apUynXCBirYcUCPMk3H9DQVwA38ZLpI=" },
            { "7.3.3_uat", "HpCI4lCwH6v6F1KLJqLvluEpOSqKp7hlPPv6VhROxSw=" },
            { "7.4.0_cert", "ui0rgix97TN8ro+/tlzRIAfhRKW1yQRrnjRp1dH0jkg=" },
            { "7.4.0_live", "hmYOV2leQ5UmEHZsScG8NGCQ8wBTqt/7KmoWLLY4Sew=" },
            { "7.4.0_ptb", "DcP6eOwcXwrqH9RyhHfqUP3GS2M1X98H+wi5mepxksE=" },
            { "7.4.0_qa", "dThHXJc2zsxAfK7K1/miYfpe7uNho+a9Up+fota67TU=" },
            { "7.4.0_stage", "mbZiqU5HrzGoBVFjoKZG0b8Om6MmH7ps1hyVznIlLlU=" },
            { "7.5.0_cert", "11w6wb3GFzPAOqFdBe2AMeRPqKypHOnXKAqtmrbXE1o=" },
            { "7.5.0_live", "GCsdX4l+1E15huC8tkCai6mnMURpPUcIC5fYy+bAHzY=" },
            { "7.5.0_ptb", "Yg0Kaz1g5clbukFwUGpVBtABEWXEQ3PyNpP+Mo3OBEA=" },
            { "7.5.0_qa", "uwnyHBf1uWfKBDI60wTg4ZL0ULXZE+E28urW6MnWfCM=" },
            { "7.5.0_stage", "wfUY2ROR3Q3Xh9oZNUPuxIINGwlLHwVMLmK1CcvSuuU=" },
            { "7.5.1_cert", "LucOFjsJnTi3G0vnMtAxLBhYkMMK/ijFj9VvFV+liL8=" },
            { "7.5.1_live", "ZChS0wDSGzHr7IWG5Sr2KOQA79sFSESyqbTYOkU71qQ=" },
            { "7.5.1_ptb", "lKY9P4Z2euaqjo1ABDCHyDB8cR7MTrTZ7anAJ/onlGA=" },
            { "7.5.1_qa", "q63tPuaYaCMMjrmdDcfc1TpDr4/BFMxFmTp/Uc9W3XM=" },
            { "7.5.1_stage", "sCjYsopR74p/jYDRk451WWlFvLGrGLkHvOnHTteNikQ=" },
            { "7.6.0_cert", "r+0dkXIxgr5eF/x0dWNP8ooRjorsBciKBbR7WFZ8jHI=" },
            { "7.6.0_live", "y1YbeilnHpduViPhlSFNocaOnM4qzmYtUtBbzd4curQ=" },
            { "7.6.0_ptb", "1+Dto68Yn10HpLkULzLSXF6HECBmra16cyXxC/eb50U=" },
            { "7.6.0_qa", "sKKuxiYG6cABCuDlsEQVRTxJeLKnTAq9D2GhTtAw6E8=" },
            { "7.6.0_stage", "0iKJcOgk9p5ZRD3SYKOLSsh+RW5qLEeLoqNP2WCovHc=" },
            { "7.7.0_cert", "wJIbB5yWNCJ/URA7AuM3YLrJXUHDbCUAcYnyt9gvce0=" },
            { "7.7.0_live", "MBCSROHTezzV2ZVSyn5C2MeWZPCYQ6BICj8VWfGZA8g=" },
            { "7.7.0_ptb", "NRbPCOOu0fbo5GRM3tLvlKcSPDRyvxRcxX/cg1b2UVY=" },
            { "7.7.0_qa", "/XNU5W8KsDikTzGMsL6FLAzcgW8U16gkCcfRrkMTSqg=" },
            { "7.7.0_stage", "1jRo6/FVpJYD0LicCLhVumFHH//DjsgkgBGL1sVQ8Fo=" },
            { "8.0.0_cert", "yem2o4Wnj0hjG+w9rgZy6/HDdzZGOQA55HIRoOqbQRI=" },
            { "8.0.0_live", "55K4rRcLqvmI4E6M4umRuG7b9dCZU2gakfydZu+QgVc=" },
            { "8.0.0_ptb", "h9hh3iQC8rBjJCKlsvoYtwVVy1RUCGbLktJWCChEfJA=" },
            { "8.0.0_qa", "nXxJPsjLK5KtDb3jV8K5cALq7mgaEJiBPwJPHpeE2tg=" },
            { "8.0.0_stage", "PzrahvpvH0YGNPLtg5K3itYucTOpD5JJRkS0ByO+VN8=" },
            { "8.1.0_cert", "vYvxa0OQn/9tumqqYQrVEBaWiInLozDZYf3fprLTrxs=" },
            { "8.1.0_live", "u/KaSJ2VbCtVyGlLK2gV7ly4xRMiLfvofTCislt0Hqo=" },
            { "8.1.0_ptb", "EnYFfS63mcoBvBBMCV6VVgkEGtVlReqa8n4bw5M4qgQ=" },
            { "8.1.0_qa", "NOqDNLSGuu/6uZ2ezNiV6Sr42gfJkS46riDX3UfEoDI=" },
            { "8.1.0_stage", "kUxPy+fwYWStoAtJHmxlL9NWNyktBr6qbeRiwup0vEg=" },
            // End of V3 encryption algorithm
            // Start of test builds? Couldn't figure decryption
            { "9999.17.0", "pGJJqK8oYVFKVSZwynOFAGAYniIZd/ycyFBZc8L7HJk=" },
            { "9999.7.0", "vGeAns6/FR+tisIgyJYAoQAaaQT8+wdKcJuKvFfCiXw=" },
            // End of test builds?
            // Start of mobile access keys, deprecated since NetEase took over mobile, used V2 encryption algorithm
            { "m_5.0.2", "BTbDF5V4GcEFSVgmdfdmb9vovbGLMkw4ZdtXuuK+IWk=" },
            { "m_5.1.0", "jI1nZdabca4x3Mynac45DQH9jvAZF2fZROl5ctn6Iao=" },
            { "m_5.1.1", "jI1nZdabca4x3Mynac45DQH9jvAZF2fZROl5ctn6Iao=" },
            { "m_5.2.0", "xP5RCJi20LH8xv+dVIlOyiJc2qxJRYG2v3jgsIpA4mM=" },
            { "m_5.2.1", "4sa7tZpejk4g09GqTswxMDzeHczz5zgKIQNs94lamfk=" },
            { "m_5.3.0", "FpGfN0mxxojdSWVEiGHS5okmfxUVtAZk8jCylbLljU4=" },
            { "m_5.4.0", "lCwFZZa+a5m1Xf8xd5kkj7IX0ak7fkbARkGiRj/qh2Y=" }
            // End of mobile access keys
        };
    }
}

// Netease config
public class NeteaseConfig
{
    public string ExtractedVersion { get; set; }
    public string NeteaseVersion { get; set; }
    public string BackupNeteaseVersion { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Branch Branch { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public Platform Platform { get; set; }
    public string PathToGameDirectory { get; set; }
    public NeteaseApiConfig ApiConfig { get; set; }

    public NeteaseConfig()
    {
        ExtractedVersion = "";
        NeteaseVersion = "";
        BackupNeteaseVersion = "";
        Branch = Branch.live;
        Platform = Platform.ios; // iOS is pretty much always updated earlier
        PathToGameDirectory = "";
        ApiConfig = new NeteaseApiConfig();
    }
}

// Sensitive config, such as credentials
public class SensitiveConfig
{
    public string S3AccessKey { get; set; }
    public string S3SecretKey { get; set; }
    public string S3BucketName { get; set; }
    public string AWSRegion { get; set; }

    public SensitiveConfig()
    {
        S3AccessKey = "";
        S3SecretKey = "";
        S3BucketName = "";
        AWSRegion = "";
    }
}

// Netease API config
public class NeteaseApiConfig
{
    public string LatestVersion { get; set; }
    public string ApiBaseUrl { get; set; }
    public string CdnBaseUrl { get; set; }
    public string CdnContentSegment { get; set; }
    public Dictionary<string, string> CdnEndpoints { get; set; }
    public Dictionary<string, string> ApiEndpoints { get; set; }

    public NeteaseApiConfig()
    {
        LatestVersion = "";
        ApiBaseUrl = "https://latest.{0}.bareasedbd.com/api/v1";
        CdnBaseUrl = "https://client-data.{0}.bareasedbd.com";
        CdnContentSegment = "/clientData/{0}/content/";
        CdnEndpoints = new()
        {
            { "catalog", "/catalog.json" },
            { "dynamicContent", "/dynamicContent/DynamicContent.json" },
            { "event", "/event.json" },
            { "specialEventsContent", "/specialEventsContent.json" }
        };
        ApiEndpoints = new() 
        {
            { "contentVersion", "/utils/contentVersion/version" }
        };
    }
}

// Game branches
public enum Branch
{
    live,
    ptb,
    dev,
    qa,
    stage,
    cert,
    uat
}

public enum Platform
{
    ios,
    android
}