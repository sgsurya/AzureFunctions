#r "Microsoft.WindowsAzure.Storage"
using System.Net;

static bool BlobExists(Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blob)
{
    try
    {
        blob.FetchAttributes();
        return true;
    }
    catch (Exception e)
    {
        return false;
    }
}

static void ReadBlob(Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blob, ref string[] fields)
{
    using (var stream = blob.OpenRead())
    {
        using (StreamReader reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                fields = reader.ReadLine().ToString().Split(';');
                break;
            }
        }
    }
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string sampleFile = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "sample", true) == 0)
        .Value;

    string fullFile = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "full", true) == 0)
       .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();


    // Set name to query string or body data
    sampleFile = sampleFile ?? data?.sample;
    fullFile = fullFile ?? data?.full;

    if (sampleFile == null)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a sample on the query string or in the request body");
    }
    else if (fullFile == null)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a full on the query string or in the request body");
    }
    else
    {
        Microsoft.WindowsAzure.Storage.Auth.StorageCredentials creds = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials("cdeappdev", "LFc/WpvofrNRInVtNYSXLmqJGtjKLremzpdxe+C9SwEd+qDpRwajQ1wNUCRDstdrettgLprscEFYxLpqSLxdcQ==");
        Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = new Microsoft.WindowsAzure.Storage.CloudStorageAccount(creds, false);

        Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container = blobClient.GetContainerReference("input");
        Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer outcontainer = blobClient.GetContainerReference("output");
        Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blockBlobSample = container.GetBlockBlobReference(sampleFile);
        Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blockBlobFull = container.GetBlockBlobReference(fullFile);

        if (!BlobExists(blockBlobSample) && !BlobExists(blockBlobFull))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest, "Error: Sample file and Full file provided are invalid.");
        }
        else if (!BlobExists(blockBlobSample))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest, "Error: Sample file provided is invalid.");
        }
        else if (!BlobExists(blockBlobFull))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest, "Error: Full file provided is invalid.");
        }

        log.Info("Sample file is available on blob");
        log.Info("Full file is available on blob");

        string[] fieldsFullFile = new string[0];
        string[] fieldsSampleFile = new string[0];
        string[] rowsFullFile = new string[0];

        log.Info("Read headers from sample - started");
        ReadBlob(blockBlobSample, ref fieldsSampleFile);
        log.Info("Read headers from sample - completed");

        log.Info("Read headers from full file - started");
        ReadBlob(blockBlobFull, ref fieldsFullFile);
        log.Info("Read headers from full file - completed");

        if (fieldsSampleFile.Length == fieldsFullFile.Length)
        {
            bool isEqual = true;
            int ind;
            for (ind = 0; ind < fieldsSampleFile.Length; ind++)
            {
                if (string.Compare(fieldsSampleFile[ind], fieldsFullFile[ind]) != 0)
                {
                    isEqual = false;
                    break;
                }
            }

            if (isEqual)
            {
                log.Info("Read records from full file - started");

                int cntRecords = 0;
                using (var stream = blockBlobFull.OpenRead())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            rowsFullFile = reader.ReadLine().ToString().Split(';');
                            cntRecords++;
                        }
                    }
                }

                log.Info("Read records from full file - completed");
                log.Info(cntRecords + "");

                return req.CreateResponse(HttpStatusCode.OK, "The file was processed. Validation Successful.");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "The file was processed. " + "Error: The files have a mismatch at column " + (ind + 1) + ". " + fieldsSampleFile[ind] + " does not match " + fieldsFullFile[ind] + ".");
            }
        }
        else
        {
            log.Error("The file was processed. Column count doesn't match.");
            return req.CreateResponse(HttpStatusCode.BadRequest, "The file was processed. Error: Column Count doesn't match");
        }
    }
}
