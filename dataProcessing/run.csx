#r "Microsoft.WindowsAzure.Storage"

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
    string name = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "id", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    name = name ?? data?.name;

    Microsoft.WindowsAzure.Storage.Auth.StorageCredentials creds = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials("cdeappdev", "LFc/WpvofrNRInVtNYSXLmqJGtjKLremzpdxe+C9SwEd+qDpRwajQ1wNUCRDstdrettgLprscEFYxLpqSLxdcQ==");
    Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = new Microsoft.WindowsAzure.Storage.CloudStorageAccount(creds, false);

    Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
    Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container = blobClient.GetContainerReference("input");
    Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer outcontainer = blobClient.GetContainerReference("output");
    Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blockBlobSample = container.GetBlockBlobReference(sampleFile);
    Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob blockBlobFull = container.GetBlockBlobReference(fullFile);

    return name == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
}
