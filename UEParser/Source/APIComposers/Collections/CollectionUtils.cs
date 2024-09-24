using System.Collections.Generic;
using System.Text.RegularExpressions;
using UEParser.Models;

namespace UEParser.APIComposers;

public class CollectionUtils
{
    public static string TransformImagePath(string input)
    {
        string pattern = @"/Game/UI/UMGAssets/Icons/Banners/CollectionBanners/(.+)\.\1";
        string transformedString = Regex.Replace(input, pattern, "/images/UI/Icons/Banners/CollectionBanners/$1.png");

        return transformedString;
    }

    public static Dictionary<string, int> CreateCollectionsDictionary(dynamic CollectionsData)
    {
        Dictionary<string, int> collectionsDictionary = [];

        for (int i = 0; i < CollectionsData.collections.Count; i++)
        {
            string collectionId = CollectionsData.collections[i].collectionId;
            collectionsDictionary[collectionId] = i;
        }

        return collectionsDictionary;
    }

    public static void PopulateLocalizationFromApi(Dictionary<string, Collection> localizedCollectionsDB, string langKey, Dictionary<string, int> collectionsDictionary, dynamic CollectionsData)
    {
        foreach (var collection in localizedCollectionsDB)
        {
            int matchingIndex = collectionsDictionary[collection.Value.CollectionId];

            var collectionTitle = CollectionsData.collections[matchingIndex].collectionTitle[langKey];
            var collectionSubTitle = CollectionsData.collections[matchingIndex].collectionSubtitle[langKey];

            collection.Value.CollectionTitle = collectionTitle;
            collection.Value.CollectionSubtitle = collectionSubTitle;
        }
    }
}