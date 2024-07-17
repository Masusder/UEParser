using System;
using System.Collections.Generic;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace UEParser.Services;

public class S3Service
{
    private readonly AmazonS3Client _s3Client;

    public S3Service(string accessKey, string secretKey, string region)
    {
        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(region));
    }

    public List<string> ListAllObjectsInFolder(string bucketName, string folderKey)
    {
        ListObjectsV2Request request = new()
        {
            BucketName = bucketName,
            Prefix = folderKey + "/"
        };

        List<string> objectKeys = [];

        do
        {
            var response = _s3Client.ListObjectsV2Async(request);

            foreach (var entry in response.Result.S3Objects)
            {
                // Add the object key to the list
                objectKeys.Add(entry.Key);
            }

            // Set the continuation token if there are more objects to retrieve
            request.ContinuationToken = response.Result.NextContinuationToken;

        } while (!string.IsNullOrEmpty(request.ContinuationToken));

        return objectKeys;
    }
}