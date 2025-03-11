using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Queryable;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mavercloud.PDF.Biz
{
    public class AzureStorageServiceBase
    {
        private string storageConnectionString;
        public AzureStorageServiceBase(string storageConnectionString)
        {
            this.storageConnectionString = storageConnectionString;
        }

        public bool FileExist(string filePath)
        {
            return FileExist(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
        }

        public bool FileExist(string containerName, string fileName)
        {
            var blobClient = new BlobClient(storageConnectionString, containerName, fileName);
            return blobClient.Exists();
        }

        public virtual string UploadFile(string containerName, string fileName, Stream fileStream, bool overwrite = true)
        {
            return UploadFile(containerName, fileName, fileStream, overwrite, string.Empty);
        }

        public virtual string UploadFile(string containerName, string fileName, byte[] byteArray, bool overwrite = true)
        {
            return UploadFile(containerName, fileName, byteArray, overwrite, string.Empty);
        }

        public string UploadFile(string containerName, string fileName, Stream fileStream, bool overwrite, string contentType)
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(storageConnectionString, containerName);
            if (!blobContainerClient.Exists())
            {
                blobContainerClient.Create(PublicAccessType.Blob);
            }

            BlobClient blob = blobContainerClient.GetBlobClient(fileName);
            if (overwrite || !blob.Exists())
            {
                blob.Upload(fileStream, overwrite);
                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = MimeMapping.MimeUtility.GetMimeMapping(fileName);
                }

                BlobProperties properties = blob.GetProperties();
                BlobHttpHeaders headers = new BlobHttpHeaders
                {
                    // Set the MIME ContentType every time the properties 
                    // are updated or the field will be cleared
                    ContentType = contentType,

                    // Populate remaining headers with 
                    // the pre-existing properties
                    CacheControl = properties.CacheControl,
                    ContentDisposition = properties.ContentDisposition,
                    ContentEncoding = properties.ContentEncoding,
                    ContentHash = properties.ContentHash
                };
                blob.SetHttpHeaders(headers);
            }



            return blob.Uri.ToString();
        }

        public string UploadFile(string containerName, string fileName, byte[] byteArray, bool overwrite, string contentType)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                return UploadFile(containerName, fileName, stream, overwrite, contentType);
            }
        }

        public virtual async Task<string> UploadFileAsync(string containerName, string fileName, Stream fileStream, bool overwrite = true)
        {
            return await UploadFileAsync(containerName, fileName, fileStream, overwrite, string.Empty);
        }

        public virtual async Task<string> UploadFileAsync(string containerName, string fileName, byte[] byteArray, bool overwrite = true)
        {
            return await UploadFileAsync(containerName, fileName, byteArray, overwrite, string.Empty);
        }

        public async Task<string> UploadFileAsync(string containerName, string fileName, Stream fileStream, bool overwrite, string contentType)
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(storageConnectionString, containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                await blobContainerClient.CreateAsync(PublicAccessType.Blob);
            }

            BlobClient blob = blobContainerClient.GetBlobClient(fileName);
            if (overwrite || !await blob.ExistsAsync())
            {
                await blob.UploadAsync(fileStream, overwrite);
                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = MimeMapping.MimeUtility.GetMimeMapping(fileName);
                }

                BlobProperties properties = await blob.GetPropertiesAsync();
                BlobHttpHeaders headers = new BlobHttpHeaders
                {
                    // Set the MIME ContentType every time the properties 
                    // are updated or the field will be cleared
                    ContentType = contentType,

                    // Populate remaining headers with 
                    // the pre-existing properties
                    CacheControl = properties.CacheControl,
                    ContentDisposition = properties.ContentDisposition,
                    ContentEncoding = properties.ContentEncoding,
                    ContentHash = properties.ContentHash
                };
                await blob.SetHttpHeadersAsync(headers);
            }



            return blob.Uri.ToString();
        }

        public async Task<string> UploadFileAsync(string containerName, string fileName, byte[] byteArray, bool overwrite, string contentType)
        {
            using (var stream = new MemoryStream(byteArray))
            {
                return await UploadFileAsync(containerName, fileName, stream, overwrite, contentType);
            }
        }


        public List<string> GetFiles(string containerName)
        {
            List<string> files = new List<string>();

            BlobContainerClient container = new BlobContainerClient(storageConnectionString, containerName);

            if (container.Exists())
            {
                foreach (BlobItem blob in container.GetBlobs())
                {
                    if (blob.Properties.BlobType == BlobType.Block)
                    {

                        files.Add(Flurl.Url.Combine(
                        container.Uri.AbsoluteUri,
                        blob.Name));
                    }
                }
            }
            return files;
        }


        public void DeleteFile(string containerName, string fileName)
        {
            var blobClient = new BlobClient(storageConnectionString, containerName, fileName);
            blobClient.DeleteIfExists();
        }

        public void DeleteFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                DeleteFile(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
            }
        }

        public void DeleteFiles(string containerName)
        {
            BlobContainerClient container = new BlobContainerClient(storageConnectionString, containerName);
            container.DeleteIfExists();
        }


        public void DownloadFileToStream(string containerName, string fileName, Stream fileStream)
        {
            var blobClient = new BlobClient(storageConnectionString, containerName, fileName);
            if (blobClient.Exists())
            {
                blobClient.DownloadTo(fileStream);
            }
        }

        public void DownloadFileToStream(string filePath, Stream fileStream)
        {
            DownloadFileToStream(Path.GetDirectoryName(filePath), Path.GetFileName(filePath), fileStream);
        }

        public byte[] DownloadFileToByteArray(string containerName, string fileName)
        {
            using (var stream = new MemoryStream())
            {
                DownloadFileToStream(containerName, fileName, stream);
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                return stream.ToArray();

            }

        }

        public byte[] DownloadFileToByteArray(string filePath)
        {
            return DownloadFileToByteArray(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));

        }








        public async Task DownloadFileToStreamAsync(string containerName, string fileName, Stream fileStream)
        {
            var blobClient = new BlobClient(storageConnectionString, containerName, fileName);
            if (blobClient.Exists())
            {
                await blobClient.DownloadToAsync(fileStream);
            }
        }

        public async Task DownloadFileToStreamAsync(string filePath, Stream fileStream)
        {
            await DownloadFileToStreamAsync(Path.GetDirectoryName(filePath), Path.GetFileName(filePath), fileStream);
        }

        public async Task<byte[]> DownloadFileToByteArrayAsync(string containerName, string fileName)
        {
            using (var stream = new MemoryStream())
            {
                await DownloadFileToStreamAsync(containerName, fileName, stream);
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                return stream.ToArray();

            }

        }

        public async Task<byte[]> DownloadFileToByteArrayAsync(string filePath)
        {
            return await DownloadFileToByteArrayAsync(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));

        }







        public string GetFileUri(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                return GetFileUri(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetFileUri(string containerName, string fileName)
        {
            var fileUri = string.Empty;
            var blobClient = new BlobClient(storageConnectionString, containerName, fileName);
            if (blobClient.Exists())
            {

                fileUri = blobClient.Uri.AbsoluteUri;
            }
            return fileUri;
        }

        public long GetFileSize(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                return GetFileSize(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
            }
            else
            {
                return 0;
            }
        }

        public long GetFileSize(string containerName, string fileName)
        {
            long fileSzie = 0;

            var blobClient = new BlobClient(storageConnectionString, containerName, fileName);
            if (blobClient.Exists())
            {
                BlobProperties blobProperties = blobClient.GetProperties();
                fileSzie = blobProperties.ContentLength;
            }
            return fileSzie;
        }

        public DateTimeOffset? GetFileLastModified(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                return GetFileLastModified(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
            }
            else
            {
                return null;
            }
        }
        public DateTimeOffset? GetFileLastModified(string containerName, string fileName)
        {
            DateTimeOffset? dateTime = null;

            var blobClient = new BlobClient(storageConnectionString, containerName, fileName);
            if (blobClient.Exists())
            {
                BlobProperties blobProperties = blobClient.GetProperties();
                dateTime = blobProperties.LastModified;
            }
            return dateTime;
        }

        public bool CopyBlob(BlobClient sourceBlob, string containerName, string newFileName)
        {
            bool success = false;

            BlobContainerClient blobContainerClient = new BlobContainerClient(storageConnectionString, containerName);
            if (!blobContainerClient.Exists())
            {
                blobContainerClient.Create(PublicAccessType.Blob);
            }

            BlobLeaseClient lease = sourceBlob.GetBlobLeaseClient();

            // Specifying -1 for the lease interval creates an infinite lease.
            lease.Acquire(TimeSpan.FromSeconds(-1));

            var newBlobClient = new BlobClient(storageConnectionString, containerName, newFileName);
            newBlobClient.StartCopyFromUri(sourceBlob.Uri);
            try
            {
                while (true)
                {
                    // Get the destination blob's properties and display the copy status.
                    BlobProperties destProperties = newBlobClient.GetProperties();
                    if (destProperties.CopyStatus != CopyStatus.Pending)
                    {
                        success = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch { }
            finally
            {
                // Update the source blob's properties.
                BlobProperties sourceProperties = sourceBlob.GetProperties();

                if (sourceProperties.LeaseState == LeaseState.Leased)
                {
                    // Break the lease on the source blob.
                    lease.Break();
                }
            }
            return success;
        }
        public bool CopyBlob(string containerName, string fileName, string newFileName)
        {
            return CopyBlob(containerName, fileName, containerName, newFileName);
        }

        public bool CopyBlob(string srcContainerName, string srcFileName, string destContainerName, string destFileName)
        {
            bool success = false;
            var blobClient = new BlobClient(storageConnectionString, srcContainerName, srcFileName);

            if (blobClient.Exists())
            {
                success = CopyBlob(blobClient, destContainerName, destFileName);
            }


            return success;
        }

        public void SetContainerAccessPolicy(string containerName, PublicAccessType accessType)
        {
            BlobContainerClient blobContainerClient = new BlobContainerClient(storageConnectionString, containerName);
            if (blobContainerClient.Exists())
            {
                blobContainerClient.SetAccessPolicy(accessType);
            }
        }

        public void SendQueueMessage(string content, string queueName, TimeSpan? visibilityTimeout, TimeSpan? timeToLive)
        {
            QueueClient queue = new QueueClient(storageConnectionString, queueName);
            queue.CreateIfNotExists();
            queue.SendMessage(content, visibilityTimeout, timeToLive);
        }

        public async Task SendQueueMessageAsync(string content, string queueName, TimeSpan? visibilityTimeout, TimeSpan? timeToLive)
        {
            QueueClient queue = new QueueClient(storageConnectionString, queueName);
            await queue.CreateIfNotExistsAsync();
            await queue.SendMessageAsync(content, visibilityTimeout, timeToLive);
        }



        public void SendQueueMessage(string content, string queueName)
        {
            QueueClient queue = new QueueClient(storageConnectionString, queueName);
            queue.CreateIfNotExists();
            queue.SendMessage(content);
        }

        public async Task SendQueueMessageAsync(string content, string queueName)
        {
            QueueClient queue = new QueueClient(storageConnectionString, queueName);
            await queue.CreateIfNotExistsAsync();
            await queue.SendMessageAsync(content);
        }

        public void SendQueueMessages(List<string> contents, string queueName, bool parallel = false)
        {
            QueueClient queue = new QueueClient(storageConnectionString, queueName);
            queue.CreateIfNotExists();

            if (parallel)
            {
                Parallel.ForEach(contents, str =>
                {
                    try
                    {
                        queue.SendMessage(str);
                    }
                    catch { }
                });
            }
            else
            {
                foreach (var str in contents)
                {
                    queue.SendMessage(str);
                }
            }
        }

        public void SendQueueMessages(List<string> contents, string queueName, TimeSpan? visibilityTimeout, TimeSpan? timeToLive, bool parallel = false)
        {
            QueueClient queue = new QueueClient(storageConnectionString, queueName);
            queue.CreateIfNotExists();

            if (parallel)
            {
                Parallel.ForEach(contents, str =>
                {
                    try
                    {
                        queue.SendMessage(str, visibilityTimeout, timeToLive);
                    }
                    catch { }
                });
            }
            else
            {
                foreach (var str in contents)
                {
                    queue.SendMessage(str, visibilityTimeout, timeToLive);
                }
            }
        }

        public void InsertTableEntity(string tableName, TableEntity entity)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            {

                TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
                table.Execute(insertOperation);
            }
        }

        public async Task InsertTableEntityAsync(string tableName, TableEntity entity)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            {

                TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
                await table.ExecuteAsync(insertOperation);
            }
        }



        public void BatchInsertTableEntities<T>(string tableName, List<T> entities) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
            if (entities.Count < 100)
            {
                try
                {
                    TableBatchOperation insertOperation = new TableBatchOperation();
                    foreach (var entity in entities)
                    {
                        insertOperation.InsertOrReplace(entity);
                    }
                    table.ExecuteBatch(insertOperation);
                }
                catch
                {
                    foreach (var entity in entities)
                    {
                        try
                        {
                            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
                            table.Execute(insertOperation);
                        }
                        catch (Exception subEx)
                        {
                        }

                    }
                }
            }
            else
            {

                int pageSize = 99;
                int startIndex = 0;
                var subEntities = entities.OrderBy(t => t.PartitionKey).Skip(startIndex).Take(pageSize).ToList();
                while (subEntities != null && subEntities.Count > 0)
                {
                    BatchInsertTableEntities<T>(tableName, subEntities);
                    startIndex += pageSize;
                    if (startIndex >= entities.Count)
                    {
                        break;
                    }
                    else
                    {
                        subEntities = entities.OrderBy(t => t.PartitionKey).Skip(startIndex).Take(pageSize).ToList();
                    }
                }

            }
        }

        public async Task BatchInsertTableEntitiesAsync<T>(string tableName, List<T> entities) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            if (entities.Count < 100)
            {
                try
                {
                    TableBatchOperation insertOperation = new TableBatchOperation();
                    foreach (var entity in entities)
                    {
                        insertOperation.InsertOrReplace(entity);
                    }
                    await table.ExecuteBatchAsync(insertOperation);
                }
                catch
                {
                    foreach (var entity in entities)
                    {
                        try
                        {
                            TableOperation insertOperation = TableOperation.InsertOrReplace(entity);
                            await table.ExecuteAsync(insertOperation);
                        }
                        catch (Exception subEx)
                        {
                        }

                    }
                }
            }
            else
            {

                int pageSize = 99;
                int startIndex = 0;
                var subEntities = entities.OrderBy(t => t.PartitionKey).Skip(startIndex).Take(pageSize).ToList();
                while (subEntities != null && subEntities.Count > 0)
                {
                    await BatchInsertTableEntitiesAsync<T>(tableName, subEntities);
                    startIndex += pageSize;
                    if (startIndex >= entities.Count)
                    {
                        break;
                    }
                    else
                    {
                        subEntities = entities.OrderBy(t => t.PartitionKey).Skip(startIndex).Take(pageSize).ToList();
                    }
                }

            }
        }

        public T RetrieveTableEntity<T>(string tableName, string partitionKey, string rowKey, List<string> columns = null) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey, columns);

            var result = table.Execute(retrieveOperation);
            var entity = result.Result;
            if (entity != null)
            {
                return (T)entity;
            }
            else
            {
                return default(T);
            }
        }

        public async Task<T> RetrieveTableEntityAsync<T>(string tableName, string partitionKey, string rowKey, List<string> columns = null) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey, columns);

            var taskResult = await table.ExecuteAsync(retrieveOperation);
            var entity = taskResult.Result;
            if (entity != null)
            {
                return (T)entity;
            }
            else
            {
                return default(T);
            }
        }

        public T RetrieveTableEntitiesWithSelectedColumns<T>(string tableName, string partitionKey, string rowKey, List<string> columns) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey, columns);
            var result = table.Execute(retrieveOperation);
            var entity = result.Result;
            if (entity != null)
            {
                return (T)entity;
            }
            else
            {
                return default(T);
            }
        }

        public async Task<T> RetrieveTableEntitiesWithSelectedColumnsAsync<T>(string tableName, string partitionKey, string rowKey, List<string> columns) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);

            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey, columns);
            var result = await table.ExecuteAsync(retrieveOperation);
            var entity = result.Result;
            if (entity != null)
            {
                return (T)entity;
            }
            else
            {
                return default(T);
            }
        }

        public List<T> RetrieveTableEntities<T>(string tableName, string partitionKey, DateTime startTime, DateTime endTime) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            return table.CreateQuery<T>().Where(t => t.PartitionKey == partitionKey && t.Timestamp >= startTime && t.Timestamp <= endTime).ToList();

        }

        public List<T> RetrieveTableEntities<T>(string tableName, DateTime startTime, DateTime endTime) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            return table.CreateQuery<T>().Where(t => t.Timestamp >= startTime && t.Timestamp < endTime).ToList();

        }

        public List<T> RetrieveTableEntities<T>(string tableName, string partitionKey) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            return table.CreateQuery<T>().Where(t => t.PartitionKey == partitionKey).ToList();
        }

        public async Task<List<T>> RetrieveTableEntitiesAsync<T>(string tableName, string partitionKey) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            var query = table.CreateQuery<T>().Where(t => t.PartitionKey == partitionKey).AsTableQuery<T>();

            var list = new List<T>();

            TableContinuationToken continuationToken = null;
            do
            {
                // Execute the query async until there is no more result
                var queryResult = await table.ExecuteQuerySegmentedAsync<T>(query, continuationToken);
                list.AddRange(queryResult.ToList());
                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            return list;
        }



        public List<T> RetrieveTableEntitiesWithSelectedColumns<T>(string tableName, string partitionKey, List<string> columns) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            var tableQuery = new TableQuery<T>();
            tableQuery.SelectColumns = columns;
            tableQuery.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            return table.ExecuteQuery<T>(tableQuery).ToList();
        }

        public async Task<List<T>> RetrieveTableEntitiesWithSelectedColumnsAsync<T>(string tableName, string partitionKey, List<string> columns) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            var tableQuery = new TableQuery<T>();
            tableQuery.SelectColumns = columns;
            tableQuery.FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);

            var list = new List<T>();

            TableContinuationToken continuationToken = null;
            do
            {
                // Execute the query async until there is no more result
                var queryResult = await table.ExecuteQuerySegmentedAsync<T>(tableQuery, continuationToken);
                list.AddRange(queryResult.ToList());
                continuationToken = queryResult.ContinuationToken;
            } while (continuationToken != null);

            return list;
        }

        public List<T> RetrieveTableEntities<T>(string tableName, string partitionKey, List<string> rowKeys) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);


            var results = new ConcurrentBag<T>();

            Parallel.ForEach(rowKeys, rowKey =>
            {
                var result = table.Execute(TableOperation.Retrieve<T>(partitionKey, rowKey));
                var entity = result.Result;
                if (entity != null)
                {
                    results.Add((T)entity);
                }
            });

            var entities = results.ToList();


            //var entities = new List<T>();
            //foreach (var rowKey in rowKeys)
            //{
            //    var result = table.Execute(TableOperation.Retrieve<T>(partitionKey, rowKey));
            //    var entity = result.Result;
            //    if (entity != null)
            //    {
            //        entities.Add((T)entity);
            //    }
            //}


            //var firstRowKey = rowKeys[0];
            //var filter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, firstRowKey);
            //if (rowKeys.Count > 1)
            //{
            //    for (int i = 0; i < rowKeys.Count; i++)
            //    {
            //        var thisFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKeys[i]);
            //        filter = TableQuery.CombineFilters(filter, TableOperators.Or, thisFilter);
            //    }
            //}
            //var entities = table.CreateQuery<T>().Where(filter).ToList();

            return entities;

        }




        public List<T> RetrieveTableEntitiesWithSelectedColumns<T>(string tableName, string partitionKey, List<string> rowKeys, List<string> columns) where T : ITableEntity, new()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);


            var results = new ConcurrentBag<T>();

            Parallel.ForEach(rowKeys, rowKey =>
            {
                var result = table.Execute(TableOperation.Retrieve<T>(partitionKey, rowKey, columns));
                var entity = result.Result;
                if (entity != null)
                {
                    results.Add((T)entity);
                }
            });

            var entities = results.ToList();


            return entities;

        }

        public void DeleteTableEntity(string tableName, ITableEntity entity)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference(tableName);
                var operation = TableOperation.Delete(entity);
                table.Execute(operation);
            }
            catch
            {
                DeleteTableEntity(tableName, entity.PartitionKey, entity.RowKey);
            }
        }

        public void DeleteTableEntity(string tableName, string partitionKey, string rowKey)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            var retrieveOperation = TableOperation.Retrieve(partitionKey, rowKey);
            var result = table.Execute(retrieveOperation);
            var entity = result.Result;
            if (entity != null)
            {
                var operation = TableOperation.Delete(entity as ITableEntity);
                table.Execute(operation);

            }
        }

        public void BatchDeleteTableEntities<T>(string tableName, List<T> entities) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            if (table.Exists())
            {
                if (entities.Count < 100)
                {
                    try
                    {
                        TableBatchOperation deleteOperation = new TableBatchOperation();
                        foreach (var entity in entities)
                        {
                            deleteOperation.Delete(entity);
                        }
                        table.ExecuteBatch(deleteOperation);
                    }
                    catch
                    {
                        foreach (var entity in entities)
                        {
                            try
                            {
                                TableOperation deleteOperation = TableOperation.Delete(entity);
                                table.Execute(deleteOperation);
                            }
                            catch (Exception subEx)
                            {
                            }

                        }
                    }
                }
                else
                {

                    int pageSize = 99;
                    int startIndex = 0;
                    var subEntities = entities.OrderBy(t => t.RowKey).Skip(startIndex).Take(pageSize).ToList();
                    while (subEntities != null && subEntities.Count > 0)
                    {
                        BatchDeleteTableEntities<T>(tableName, subEntities);
                        startIndex += pageSize;
                        if (startIndex >= entities.Count)
                        {
                            break;
                        }
                        else
                        {
                            subEntities = entities.OrderBy(t => t.RowKey).Skip(startIndex).Take(pageSize).ToList();
                        }
                    }

                }
            }
        }

        public async Task BatchDeleteTableEntitiesAsync<T>(string tableName, List<T> entities) where T : ITableEntity
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(tableName);
            if (table.Exists())
            {
                if (entities.Count < 100)
                {
                    try
                    {
                        TableBatchOperation deleteOperation = new TableBatchOperation();
                        foreach (var entity in entities)
                        {
                            deleteOperation.Delete(entity);
                        }
                        await table.ExecuteBatchAsync(deleteOperation);
                    }
                    catch
                    {
                        foreach (var entity in entities)
                        {
                            try
                            {
                                TableOperation deleteOperation = TableOperation.Delete(entity);
                                await table.ExecuteAsync(deleteOperation);
                            }
                            catch (Exception subEx)
                            {
                            }

                        }
                    }
                }
                else
                {

                    int pageSize = 99;
                    int startIndex = 0;
                    var subEntities = entities.OrderBy(t => t.RowKey).Skip(startIndex).Take(pageSize).ToList();
                    while (subEntities != null && subEntities.Count > 0)
                    {
                        await BatchDeleteTableEntitiesAsync<T>(tableName, subEntities);
                        startIndex += pageSize;
                        if (startIndex >= entities.Count)
                        {
                            break;
                        }
                        else
                        {
                            subEntities = entities.OrderBy(t => t.RowKey).Skip(startIndex).Take(pageSize).ToList();
                        }
                    }

                }
            }
        }



        //public string GetQueueMessage(string queueName)
        //{
        //    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
        //    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        //    CloudQueue queue = queueClient.GetQueueReference(queueName);
        //    CloudQueueMessage retrievedMessage = queue.GetMessage();
        //    if (retrievedMessage != null)
        //    {
        //        return retrievedMessage.AsString;
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
    }
}
