using System;
using ShellProgressBar;

namespace Dominosoft.Azure.Blob
{
  class Progressor
  {
    private ProgressBar MainProgressBar { get; }
    public Progressor(int fileCount)
    {
      var options = new ProgressBarOptions
      {
        ForegroundColor = ConsoleColor.Yellow,
        BackgroundColor = ConsoleColor.DarkGray,
        ProgressCharacter = '─',
        EnableTaskBarProgress = true,
        ShowEstimatedDuration = false
      };
      this.MainProgressBar = new ProgressBar(fileCount, "Total File Count", options);
    }
    public void Tick()
    {
      this.MainProgressBar.Tick($"{this.MainProgressBar.CurrentTick + 1} / {this.MainProgressBar.MaxTicks} Total File Count");
    }
    public IProgress<long> CreateUploadProgress(int fileSize, string blobPath)
    {
      var chileProgressBar = this.MainProgressBar.Spawn(fileSize, blobPath, new ProgressBarOptions
      {
        ForegroundColor = ConsoleColor.Green,
        BackgroundColor = ConsoleColor.DarkGray,
        ProgressCharacter = '─',
        EnableTaskBarProgress = true,
        ShowEstimatedDuration = false,
        CollapseWhenFinished = true,
      });
      return chileProgressBar.AsProgress((long value) => $"{fileSize / (1024 * 1024.0):0.00}MB {blobPath}",
        (long value) => value / (double)fileSize); 
    }
  }
}
