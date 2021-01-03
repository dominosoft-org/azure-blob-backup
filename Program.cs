using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Dominosoft.Azure.Blob
{
  class Program
  {
    static int Main(string[] args)
    {
      var cts = new CancellationTokenSource();
      Console.CancelKeyPress += (s, e) =>
      {
        e.Cancel = true;
        cts.Cancel();
      };
      return MainAsync(args, cts.Token).GetAwaiter().GetResult();
    }
    static Task<int> MainAsync(string[] args, CancellationToken token)
    {
      var blobClient = new Uploader(ConfigurationManager.AppSettings["BackupDirectory"]);
      return blobClient.Start(token);
    }
  }
}
