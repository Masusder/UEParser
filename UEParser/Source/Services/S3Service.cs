using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using UEParser.ViewModels;
using UEParser.Utils;

namespace UEParser.Services;

public class S3Service
{
    private readonly AmazonS3Client _s3Client;

    public S3Service(string accessKey, string secretKey, string region)
    {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(region));
    }

    public static S3Service CreateFromConfig()
    {
        var config = ConfigurationService.Config;
        var accessKey = config.Sensitive.S3AccessKey;
        var secretKey = config.Sensitive.S3SecretKey;
        var region = config.Sensitive.AWSRegion;

        if (accessKey == null || secretKey == null || region == null) throw new Exception("Access credentials to S3 service are missing.");

        return new S3Service(accessKey, secretKey, region);
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
                objectKeys.Add(entry.Key);
            }

            // Set the continuation token if there are more objects to retrieve
            request.ContinuationToken = response.Result.NextContinuationToken;

        } while (!string.IsNullOrEmpty(request.ContinuationToken));

        return objectKeys;
    }

    public async Task UploadFileAsync(string bucketName, string key, string filePath)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                FilePath = filePath
            };

            PutObjectResponse response = await _s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                LogsWindowViewModel.Instance.AddLog($"File uploaded successfully: {filePath}", Logger.LogTags.Info);
            }
        }
        catch (AmazonS3Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"AWS S3 Error encountered on server: {e.Message}", Logger.LogTags.Error);
        }
        catch (Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"Error: {e.Message}", Logger.LogTags.Error);
        }
    }

    public async Task<CopyObjectResponse> CopyObjectAsync(string sourceBucket, string sourceKey, string destinationBucket, string destinationKey)
    {
        try
        {
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = sourceBucket,
                SourceKey = sourceKey,
                DestinationBucket = destinationBucket,
                DestinationKey = destinationKey
            };

            var copyResponse = await _s3Client.CopyObjectAsync(copyRequest);

            if (copyResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                LogsWindowViewModel.Instance.AddLog($"Successfully copied object from {sourceKey} to {destinationKey}", Logger.LogTags.Info);
            }

            return copyResponse;
        }
        catch (AmazonS3Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"AWS S3 Error encountered during copy operation: {e.Message}", Logger.LogTags.Error);
            throw;
        }
        catch (Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"Error during copy operation: {e.Message}", Logger.LogTags.Error);
            throw;
        }
    }

    public async Task DeleteObjectAsync(string bucketName, string objectKey)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var deleteResponse = await _s3Client.DeleteObjectAsync(deleteRequest);

            if (deleteResponse.HttpStatusCode == System.Net.HttpStatusCode.NoContent || 
                deleteResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                LogsWindowViewModel.Instance.AddLog($"Successfully deleted object: {objectKey}", Logger.LogTags.Info);
            }
            else
            {
                LogsWindowViewModel.Instance.AddLog($"Failed to delete object: {objectKey}. HTTP Status: {deleteResponse.HttpStatusCode}", Logger.LogTags.Error);
            }
        }
        catch (AmazonS3Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"AWS S3 Error encountered on server: {e.Message}", Logger.LogTags.Error);
        }
        catch (Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"Error: {e.Message}", Logger.LogTags.Error);
        }
    }


    public async Task UploadFileWithProgressAsync(string bucketName, string key, string filePath)
    {
        try
        {
            var transferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                FilePath = filePath
            };

            uploadRequest.UploadProgressEvent += new EventHandler<UploadProgressArgs>(UploadProgressCallback);

            await transferUtility.UploadAsync(uploadRequest);
            LogsWindowViewModel.Instance.AddLog($"File uploaded successfully: {filePath}", Logger.LogTags.Info);
        }
        catch (AmazonS3Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"AWS S3 Error encountered on server: {e.Message}", Logger.LogTags.Error);
        }
        catch (Exception e)
        {
            LogsWindowViewModel.Instance.AddLog($"Error: {e.Message}", Logger.LogTags.Error);
        }
    }

    private void UploadProgressCallback(object? sender, UploadProgressArgs e)
    {
        LogsWindowViewModel.Instance.AddLog($"Upload progress: {e.TransferredBytes}/{e.TotalBytes} ({e.PercentDone}%)", Logger.LogTags.Info);
    }

    private static void UploadDirectoryProgressCallback(object? _, UploadDirectoryProgressArgs e, string uploadDirectory)
    {
        double percentage = (double)e.NumberOfFilesUploaded / e.TotalNumberOfFiles * 100;
        string formattedBytes = StringUtils.FormatBytes(e.TransferredBytes);
        string formattedTotalBytes = StringUtils.FormatBytes(e.TotalBytes);

        string strippedUploadDirectory = StringUtils.StripDynamicDirectory(uploadDirectory, GlobalVariables.RootDir);
        var logMessage = $"Uploading from directory: {strippedUploadDirectory}\n" +
                 $"• Total number of files to upload: {e.TotalNumberOfFiles} ({formattedTotalBytes} total)\n" +
                 $"• Number of files uploaded: {e.NumberOfFilesUploaded} ({percentage:F2}%)\n" +
                 $"• Bytes uploaded so far: {formattedBytes}";
        LogsWindowViewModel.Instance.UpdateLog(logMessage, Logger.LogTags.Info);
    }

    public async Task UploadDirectoryAsync(string bucketName, string directoryPath, string s3DirectoryPath)
    {
        try
        {
            var transferUtility = new TransferUtility(_s3Client);

            var uploadDirectoryRequest = new TransferUtilityUploadDirectoryRequest
            {
                BucketName = bucketName,
                Directory = directoryPath,
                KeyPrefix = s3DirectoryPath,
                SearchOption = SearchOption.AllDirectories,
                UploadFilesConcurrently = true
            };

            uploadDirectoryRequest.UploadDirectoryProgressEvent += (sender, args) =>
            UploadDirectoryProgressCallback(sender, args, directoryPath);

            await transferUtility.UploadDirectoryAsync(uploadDirectoryRequest);
            LogsWindowViewModel.Instance.AddLog("Directory uploaded successfully.", Logger.LogTags.Info);
        }
        catch (AmazonS3Exception e)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"AWS S3 Error encountered on server: {e.Message}", Logger.LogTags.Error);
        }
        catch (Exception e)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error: {e.Message}", Logger.LogTags.Error);
        }
    }
}