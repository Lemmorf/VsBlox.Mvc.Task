using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace VsBlox.Mvc.Task
{
  /// <summary>
  /// Interface for background tasks.
  /// </summary>
  public interface ITask
  {
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds.
    /// </summary>
    /// <value>
    /// The interval in seconds.
    /// </value>
    int IntervalInSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can kick start.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance can kick start; otherwise, <c>false</c>.
    /// </value>
    bool CanKickStart { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can start.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance can start; otherwise, <c>false</c>.
    /// </value>
    bool CanStart { get; set; }

    /// <summary>
    /// Runs this instance.
    /// </summary>
    void Run();

    /// <summary>
    /// Does the work.
    /// </summary>
    void DoWork();
  }

  /// <summary>
  /// Class to manage ITask derived background tasks.
  /// 
  /// </summary>
  public class TaskConfig
  {
    /// <summary>
    /// Function to instantiate and start all ITask derived classes in the current application.
    /// </summary>
    /// <example>
    /// Usage:
    /// <code>
    ///protected void Application_Start()
    ///{
    ///     ...
    ///     TaskConfig.RegisterTasks();
    ///     ...
    ///}
    /// </code>
    /// </example>
    public static void RegisterTasks()
    {
      var tasks = InstantiateAllTasks();
      ScheduleTasks(tasks);
    }

    /// <summary>
    /// Instantiate tasks by examine assemblies and search for ITask implementations.
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<object> InstantiateAllTasks()
    {
      var interfaceType = typeof(ITask);

      var tasks = new List<object>();

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.GlobalAssemblyCache))
      {
        Type[] types;

        try { types = assembly.GetTypes(); }
        catch (Exception) { continue; }

        tasks.AddRange(from type in types where interfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract select Activator.CreateInstance(type));
      }

      return tasks;
    }

    /// <summary>
    /// Runs the task.
    /// </summary>
    /// <param name="taskName">Name of the task.</param>
    public static void RunTask(string taskName)
    {
      var interfaceType = typeof(ITask);

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.GlobalAssemblyCache))
      {
        Type[] types;

        try { types = assembly.GetTypes(); }
        catch (Exception) { continue; }

        foreach (var task in from type in types where interfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract where type.Name == taskName select Activator.CreateInstance(type) as ITask)
        {
          task?.Run();
          return;
        }
      }
    }


    /// <summary>
    /// Schedule all found tasks.
    /// </summary>
    /// <param name="tasks">List of instantiated tasks.</param>
    private static void ScheduleTasks(IEnumerable<object> tasks)
    {
      foreach (var task in tasks.Cast<ITask>().Where(task => task.CanStart))
      {
        AddTaskToCache(task, task.CanKickStart ? 1 : task.IntervalInSeconds);
      }
    }

    /// <summary>
    /// Add the specified task to the Http runtime cache with the timeout value specified in the task. 
    /// When the cache item times out, the ITask.Run method will be invoked.
    /// After completion, the task will be readded to the cache.
    /// </summary>
    /// <param name="task">Task to add to the cache.</param>
    /// <param name="intervalInSeconds"></param>
    private static void AddTaskToCache(ITask task, int intervalInSeconds)
    {
      HttpRuntime.Cache.Add(task.Name, task, null, DateTime.MaxValue, TimeSpan.FromSeconds(intervalInSeconds),
        CacheItemPriority.Normal, (key, value, reason) =>
        {
          var taskToRun = ((ITask)value);
          taskToRun.Run();
          AddTaskToCache(taskToRun, taskToRun.IntervalInSeconds);
        });
    }
  }
}