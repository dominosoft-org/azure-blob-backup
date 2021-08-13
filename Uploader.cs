using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Dominosoft.Azure.Blob
{
  class Uploader
  {
    private readonly string LocalDirectory;
    private bool RequestToQuit { get; set; }
    public Uploader(string localDirectory)
    {
      if (!Directory.Exists(localDirectory)) throw new DirectoryNotFoundException(localDirectory);
      LocalDirectory = localDirectory;
    }
    public async Task<int> Start(CancellationToken token)
    {
      RequestToQuit = false;
      token.Register(() => RequestToQuit = true);
      try
      {
        await Upload(token).ConfigureAwait(true);
      }
      catch (Exception exp)
      {
        Console.WriteLine(exp.Message);
        return exp.HResult;
      }
      return 0;
    }
    private async Task Upload(CancellationToken token)
    {
      var container = new BlobContainerClient(
        ConfigurationManager.AppSettings["BlobConnectionString"],
        ConfigurationManager.AppSettings["BlobContainerName"]);
      var subDirectoriesSettings = ConfigurationManager.AppSettings["BackupSubDirectories"];
      var subDirectories = string.IsNullOrEmpty(subDirectoriesSettings) ? null : subDirectoriesSettings.Split(',');
      var containerExists = await container.ExistsAsync(token);
      await container.CreateIfNotExistsAsync(cancellationToken: token);
      var files = Directory.GetFiles(this.LocalDirectory, "*", SearchOption.AllDirectories)
        .Where(e => subDirectories == null || subDirectories.Contains(e.Replace(this.LocalDirectory, string.Empty).Split('\\', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()))
        .Select(e => new { FilePath = e, BlobPath = ConvertBlobPath(e) }).ToList();
      if (containerExists.Value)
      {
        var currentBlobItems = container.GetBlobsAsync(cancellationToken: token);
        await foreach (var currentBlobItem in currentBlobItems)
        {
          var fileItem = files.Where(e => e.BlobPath == currentBlobItem.Name).FirstOrDefault();
          if (fileItem != null) files.Remove(fileItem);
        }
      }
      if (!files.Any()) return;
      var progressor = new Progressor(files.Count);
      foreach (var file in files)
      {
        var blob = container.GetBlobClient(file.BlobPath);
        using var stream = File.OpenRead(file.FilePath);
        await blob.UploadAsync(stream, new BlobUploadOptions()
        {
          AccessTier = AccessTier.Archive,
          ProgressHandler = progressor.CreateUploadProgress((int)stream.Length, file.BlobPath)
        }, token);
        var properties = await blob.GetPropertiesAsync(cancellationToken: token);
        Debug.Assert(stream.Length == properties.Value.ContentLength);
        progressor.Tick();
      }
    }

    private string ConvertBlobPath(string filePath)
    {
      var blobPath = filePath.Remove(0, this.LocalDirectory.Length).Replace('\\', '/');
      if ((blobPath.Length > 0) && blobPath[0] == '/') return blobPath.Remove(0, 1);
      return blobPath;
    }
  }
}
