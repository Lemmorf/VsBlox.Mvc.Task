using System;
using System.Diagnostics;
using System.Web.Hosting;

namespace VsBlox.Mvc.Task
{
  /// <summary>
  /// Base class for all ITask implementations.
  /// </summary>
  /// <example>
  /// Make your own class derived from BaseTask. 
  /// Fill in "Name", "IntervalInSeconds", "CanKickStart" and "CanStart".
  /// Override "DoWork" with your implementation of the background task.
  /// <code>
  /// </code>
  /// </example>
  public abstract class BaseTask : ITask, IRegisteredObject
  {
    /// <summary>
    /// Log Level
    /// </summary>
    public enum LogLevel
    {
      /// <summary>
      /// The information
      /// </summary>
      Info,
      /// <summary>
      /// The warning
      /// </summary>
      Warning,
      /// <summary>
      /// The error
      /// </summary>
      Error,
      /// <summary>
      /// The fatal
      /// </summary>
      Fatal
    }
    /// <summary>
    /// Log handler
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="message">The message.</param>
    /// <param name="ex">The ex.</param>
    public delegate void LogHandler(object sender, LogLevel logLevel, string message, Exception ex = null);

    /// <summary>
    /// Occurs when [log].
    /// </summary>
    public event LogHandler Log;

    private readonly object _lock = new object();
    private bool _shuttingDown;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTask"/> class.
    /// </summary>
    protected BaseTask()
    {
      HostingEnvironment.RegisterObject(this);
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public virtual string Name { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds.
    /// </summary>
    /// <value>
    /// The interval in seconds.
    /// </value>
    public virtual int IntervalInSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can kick start.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance can kick start; otherwise, <c>false</c>.
    /// </value>
    public virtual bool CanKickStart { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can start.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance can start; otherwise, <c>false</c>.
    /// </value>
    public virtual bool CanStart { get; set; }

    /// <summary>
    /// Does the work. DoWork is called from Run().
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public virtual void DoWork()
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Runs this instance.
    /// </summary>
    public void Run()
    {
      OnLog(LogLevel.Info, "Run task");

      lock (_lock)
      {
        if (_shuttingDown) return;
        DoWork();
      }
    }

    /// <summary>
    /// Requests a registered object to unregister.
    /// </summary>
    /// <param name="immediate">true to indicate the registered object should unregister from the hosting environment before returning; otherwise, false.</param>
    public void Stop(bool immediate)
    {
      OnLog(LogLevel.Info, "Stop: immediate=" + (immediate ? "true" : "false"));

      lock (_lock)
      {
        _shuttingDown = true;
      }

      HostingEnvironment.UnregisterObject(this);
    }

    /// <summary>
    /// User defined callback. Will be called for logging.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="message">Message passed to the logger</param>
    /// <param name="ex">Original exception</param>
    /// <exception cref="Exception">Writes the exception to the output console and rethrows any exception.</exception>
    protected void OnLog(LogLevel logLevel, string message, Exception ex = null)
    {
      try
      {
        Log?.Invoke(this, logLevel, message, ex);
      }
      catch (Exception ex1)
      {
        Trace.WriteLine(ex1.GetBaseException().Message);
        throw;
      }
    }
  }
}