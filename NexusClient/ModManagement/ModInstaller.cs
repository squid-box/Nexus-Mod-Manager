namespace Nexus.Client.ModManagement
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;
    using System.Threading;
    using System.Xml.Linq;
    using ChinhDo.Transactions;

    using Nexus.Client.BackgroundTasks;
    using Nexus.Client.Games;
    using Nexus.Client.ModManagement.InstallationLog;
    using Nexus.Client.ModManagement.Scripting;
    using Nexus.Client.Mods;
    using Nexus.Client.PluginManagement;
    using Nexus.Client.Util;
    using Nexus.Client.Util.Threading;
    using Nexus.Transactions;
    using Nexus.Client.Util.Collections;

    /// <summary>
    /// This installs mods.
    /// </summary>
    public class ModInstaller : ModInstallerBase
	{
		private readonly ConfirmItemOverwriteDelegate _overwriteConfirmationDelegate = null;

		#region Properties

		/// <summary>
		/// Gets or sets the mod being installed.
		/// </summary>
		/// <value>The mod being installed.</value>
		protected IMod Mod { get; set; }

		/// <summary>
		/// Gets or sets the mod name.
		/// </summary>
		/// <value>The mod name.</value>
		public string ModName => Mod?.ModName;

	    /// <summary>
		/// Gets or sets the mod file name.
		/// </summary>
		/// <value>The mod file name.</value>
		public string ModFileName => Mod?.Filename;

	    /// <summary>
		/// Gets or sets the application's envrionment info.
		/// </summary>
		/// <value>The application's envrionment info.</value>
		protected IEnvironmentInfo EnvironmentInfo { get; set; }

		/// <summary>
		/// Gets the mod activator to use to manage file installation.
		/// </summary>
		/// <value>The mod activator to use to manage file installation.</value>
		protected IVirtualModActivator VirtualModActivator { get; private set; }

		/// <summary>
		/// Gets the profile manager.
		/// </summary>
		/// <value>The profile manager.</value>
		protected IProfileManager ProfileManager { get; private set; }

		/// <summary>
		/// Gets the current game mode.
		/// </summary>
		/// <value>The the current game mode.</value>
		protected IGameMode GameMode { get; private set; }

		/// <summary>
		/// Gets the manager to use to manage plugins.
		/// </summary>
		/// <value>The manager to use to manage plugins.</value>
		protected IPluginManager PluginManager { get; private set; }

		/// <summary>
		/// Gets the install log that tracks mod install info
		/// for the current game mode.
		/// </summary>
		/// <value>The install log that tracks mod install info
		/// for the current game mode.</value>
		protected IInstallLog ModInstallLog { get; private set; }

		/// <summary>
		/// Gets or sets the file utility class.
		/// </summary>
		/// <value>The file utility class.</value>
		protected FileUtil FileUtility { get; set; }

		/// <summary>
		/// Gets the <see cref="SynchronizationContext"/> to use to marshall UI interactions to the UI thread.
		/// </summary>
		/// <value>The <see cref="SynchronizationContext"/> to use to marshall UI interactions to the UI thread.</value>
		protected SynchronizationContext UIContext { get; private set; }

		protected ReadOnlyObservableList<IMod> ActiveMods { get; set; }

        #endregion

        #region Constructors

	    /// <summary>
	    /// A simple constructor that initializes the object with the given values.
	    /// </summary>
	    /// <param name="mod">The mod being installed.</param>
	    /// <param name="gameMode">The current game mode.</param>
	    /// <param name="environmentInfo">The application's envrionment info.</param>
	    /// <param name="fileUtility">The file utility class.</param>
	    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> to use to marshall UI interactions to the UI thread.</param>
	    /// <param name="installLog">The install log that tracks mod install info for the current game mode</param>
	    /// <param name="pluginManager">The plugin manager.</param>
	    /// <param name="virtualModActivator">The virtual mod activator.</param>
	    /// <param name="profileManager">The profile manager.</param>
	    /// <param name="overwriteConfirmationDelegate">The method to call in order to confirm an overwrite.</param>
	    /// <param name="activeMods">The list of active mods.</param>
	    public ModInstaller(IMod mod, IGameMode gameMode, IEnvironmentInfo environmentInfo, FileUtil fileUtility, SynchronizationContext synchronizationContext, IInstallLog installLog, IPluginManager pluginManager, IVirtualModActivator virtualModActivator, IProfileManager profileManager, ConfirmItemOverwriteDelegate overwriteConfirmationDelegate, ReadOnlyObservableList<IMod> activeMods)
		{
			Mod = mod;
			GameMode = gameMode;
			EnvironmentInfo = environmentInfo;
			FileUtility = fileUtility;
			UIContext = synchronizationContext;
			ModInstallLog = installLog;
			PluginManager = pluginManager;
			VirtualModActivator = virtualModActivator;
			ProfileManager = profileManager;
			_overwriteConfirmationDelegate = overwriteConfirmationDelegate;
			ActiveMods = activeMods;
		}

		#endregion

		/// <summary>
		/// Installs the mod.
		/// </summary>
		public void Install()
		{
			var thdWorker = new TrackedThread(RunTasks);
			thdWorker.Thread.IsBackground = false;
			thdWorker.Start();
		}

		/// <summary>
		/// Runs the install tasks.
		/// </summary>
		protected void RunTasks()
		{
            //the install process modifies INI and config files.
            // if multiple sources (i.e., installs) try to modify
            // these files simultaneously the outcome is not well known
            // (e.g., one install changes SETTING1 in a config file to valueA
            // while simultaneously another install changes SETTING1 in the
            // file to value2 - after each install commits its changes it is
            // not clear what the value of SETTING1 will be).
            // as a result, we only allow one mod to be installed at a time,
            // hence the lock.

		    var success = false;
			var message = "The mod was not activated.";
			
			try
			{
				lock (objInstallLock)
				{
					using (var tsTransaction = new TransactionScope())
					{
						if (!File.Exists(Mod.Filename))
                        {
                            throw new Exception("The selected file was not found: " + Mod.Filename);
                        }

                        var tfmFileManager = new TxFileManager();

						if (!BeginModReadOnlyTransaction())
                        {
                            return;
                        }

                        RegisterMod();
						success = RunScript(tfmFileManager);

					    if (success)
						{
							Mod.InstallDate = DateTime.Now.ToString(CultureInfo.InvariantCulture);
							tsTransaction.Complete();
							VirtualModActivator.SaveList(true);
							message = "The mod was successfully activated.";
							GC.GetTotalMemory(true);
						}
					}
				}
			}
			catch (TransactionException)
			{
				throw;
			}
			catch (SecurityException)
			{
				throw;
			}
			catch (ObjectDisposedException)
			{
				throw;
			}

            //this blobck used to be conditionally excluded from debug builds,
            // presumably so that the debugger would break on the source of the
            // exception, however that prevents the full mod install flow from
            // happening, which lead to missed bugs

			catch (Exception e)
			{
				success = false;

				var errorMessage = new StringBuilder(e.Message);

			    switch (e)
			    {
			        case FileNotFoundException _:
			            errorMessage.Append(" (" + ((FileNotFoundException)e).FileName + ")");
			            break;
			        case IllegalFilePathException _:
			            errorMessage.Append(" (" + ((IllegalFilePathException)e).Path + ")");
			            break;
			    }

			    if (e.InnerException != null)
                {
                    errorMessage.AppendLine().AppendLine(e.InnerException.Message);
                }

                if (e is RollbackException exception)
                {
                    foreach (var exceptedResourceManager in exception.ExceptedResourceManagers)
					{
						errorMessage.AppendLine(exceptedResourceManager.ResourceManager.ToString());
						errorMessage.AppendLine(exceptedResourceManager.Exception.Message);

					    if (exceptedResourceManager.Exception.InnerException != null)
                        {
                            errorMessage.AppendLine(exceptedResourceManager.Exception.InnerException.Message);
                        }
                    }
                }

                var exceptionMessageFormat = "A problem occurred during install: " + Environment.NewLine + "{0}" + Environment.NewLine + "The mod was not installed."; ;
				message = string.Format(exceptionMessageFormat, errorMessage);
				PopupErrorMessage = message;
				PopupErrorMessageType = "Error";

                Trace.TraceError("A problem occurred during mod installation, see following log for more info.");
                TraceUtil.TraceException(e);
			}
			finally
			{
				Mod.EndReadOnlyTransaction();
			}

			OnTaskSetCompleted(success, message, Mod);
		}

		/// <summary>
		/// Puts the mod into read-only mode.
		/// </summary>
		/// <returns><c>true</c> if the the read only mode started;
		/// <c>false</c> otherwise.</returns>
		private bool BeginModReadOnlyTransaction()
		{
			var prepareModTask = new PrepareModTask(FileUtility);
			OnTaskStarted(prepareModTask);

			return prepareModTask.PrepareMod(Mod);
		}

		#region Script Execution

		/// <summary>
		/// This executes the install script.
		/// </summary>
		/// <param name="txFileManager">The transactional file manager to use to interact with the file system.</param>
		/// <returns><c>true</c> if the script completed successfully;
		/// <c>false</c> otherwise.</returns>
		protected bool RunScript(TxFileManager txFileManager)
		{
			var modFileInstaller = CreateFileInstaller(txFileManager, _overwriteConfirmationDelegate);
			var result = false;
			IIniInstaller iniInstaller = null;
			IGameSpecificValueInstaller gameSpecificValueInstaller = null;

		    if (Mod.HasInstallScript)
			{
				if (CheckScriptedModLog())
                {
                    result = RunBasicInstallScript(modFileInstaller, ActiveMods, LoadXMLModFilesToInstall());
                }
                else
				{
					try
					{
						IDataFileUtil dataFileUtility = new DataFileUtil(GameMode.GameModeEnvironmentInfo.InstallationPath);

						iniInstaller = CreateIniInstaller(txFileManager, _overwriteConfirmationDelegate);
						gameSpecificValueInstaller = CreateGameSpecificValueInstaller(txFileManager, _overwriteConfirmationDelegate);

						var installerGroup = new InstallerGroup(dataFileUtility, modFileInstaller, iniInstaller, gameSpecificValueInstaller, PluginManager);
						var scriptExecutor = Mod.InstallScript.Type.CreateExecutor(Mod, GameMode, EnvironmentInfo, VirtualModActivator, installerGroup, UIContext);
						scriptExecutor.TaskStarted += ScriptExecutor_TaskStarted;
						scriptExecutor.TaskSetCompleted += ScriptExecutor_TaskSetCompleted;
						result = scriptExecutor.Execute(Mod.InstallScript);
					}
					catch (Exception ex)
					{
						PopupErrorMessage = ex.Message;
						PopupErrorMessageType = "Error";
					}

					iniInstaller.FinalizeInstall();

				    gameSpecificValueInstaller?.FinalizeInstall();
				}
			}
			else
            {
                result = RunBasicInstallScript(modFileInstaller, ActiveMods, null);
            }

            modFileInstaller.FinalizeInstall();

			return result;
		}

		/// <summary>
		/// Handles the <see cref="IBackgroundTaskSet.TaskSetCompleted"/> event of script executors.
		/// </summary>
		/// <remarks>
		/// This unwires our listeners from the executor.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">A <see cref="TaskSetCompletedEventArgs"/> describing the event arguments.</param>
		private void ScriptExecutor_TaskSetCompleted(object sender, TaskSetCompletedEventArgs e)
		{
			var backgroundTaskExecutor = (IBackgroundTaskSet)sender;
			backgroundTaskExecutor.TaskStarted -= ScriptExecutor_TaskStarted;
			backgroundTaskExecutor.TaskSetCompleted -= ScriptExecutor_TaskSetCompleted;
		}

		/// <summary>
		/// Handles the <see cref="IBackgroundTaskSet.TaskStarted"/> event of script executors.
		/// </summary>
		/// <remarks>
		/// This bubbles the started task to any listeners.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">An <see cref="EventArgs{IBackgroundTask}"/> describing the event arguments.</param>
		private void ScriptExecutor_TaskStarted(object sender, EventArgs<IBackgroundTask> e)
		{
			OnTaskStarted(e.Argument);
		}

		/// <summary>
		/// Runs the basic install script.
		/// </summary>
		/// <remarks>
		/// A basic install installs all of the files in the mod to the installation directory,
		/// and activates all plugin files.
		/// </remarks>
		/// <param name="modFileInstaller">The file installer to use.</param>
		/// <param name="activeMods">The list of active mods.</param>
		/// <param name="installFiles">The list of specific files to install, if null the mod will be installed as usual.</param>
		/// <returns><c>true</c> if the installation was successful;
		/// <c>false</c> otherwise.</returns>
		protected bool RunBasicInstallScript(IModFileInstaller modFileInstaller, ReadOnlyObservableList<IMod> activeMods, List<KeyValuePair<string, string>> installFiles)
		{
			var basicInstallTask = new BasicInstallTask(Mod, GameMode, modFileInstaller, PluginManager, VirtualModActivator, EnvironmentInfo.Settings.SkipReadmeFiles, activeMods, installFiles);
			OnTaskStarted(basicInstallTask);

		    return basicInstallTask.Execute();
		}

        #endregion

        #region Installer Creation

        /// <summary>
        /// Creates the file installer to use to install the mod's files.
        /// </summary>
        /// <remarks>
        /// This returns the regular <see cref="ModFileInstaller"/>.
        /// </remarks>
        /// <param name="txFileManager">The transactional file manager to use to interact with the file system.</param>
        /// <param name="overwriteConfirmationDelegate">The method to call in order to confirm an overwrite.</param>
        /// <returns>The file installer to use to install the mod's files.</returns>
        protected virtual IModFileInstaller CreateFileInstaller(TxFileManager txFileManager, ConfirmItemOverwriteDelegate overwriteConfirmationDelegate)
		{
			return new ModFileInstaller(GameMode.GameModeEnvironmentInfo, Mod, ModInstallLog, PluginManager, new DataFileUtil(GameMode.GameModeEnvironmentInfo.InstallationPath), txFileManager, overwriteConfirmationDelegate, GameMode.UsesPlugins, EnvironmentInfo);
		}

		/// <summary>
		/// Creates the file installer to use to install the mod's ini edits.
		/// </summary>
		/// <remarks>
		/// This returns the regular <see cref="IniInstaller"/>.
		/// </remarks>
		/// <param name="txFileManager">The transactional file manager to use to interact with the file system.</param>
		/// <param name="overwriteConfirmationDelegate">The method to call in order to confirm an overwrite.</param>
		/// <returns>The file installer to use to install the mod's files.</returns>
		protected virtual IIniInstaller CreateIniInstaller(TxFileManager txFileManager, ConfirmItemOverwriteDelegate overwriteConfirmationDelegate)
		{
			return new IniInstaller(Mod, ModInstallLog, VirtualModActivator, txFileManager, overwriteConfirmationDelegate);
		}

		/// <summary>
		/// Creates the file installer to use to install the mod's game specific value edits.
		/// </summary>
		/// <remarks>
		/// This returns a regular <see cref="IGameSpecificValueInstaller"/>.
		/// </remarks>
		/// <param name="txFileManager">The transactional file manager to use to interact with the file system.</param>
		/// <param name="overwriteConfirmationDelegate">The method to call in order to confirm an overwrite.</param>
		/// <returns>The file installer to use to install the mod's files.</returns>
		protected virtual IGameSpecificValueInstaller CreateGameSpecificValueInstaller(TxFileManager txFileManager, ConfirmItemOverwriteDelegate overwriteConfirmationDelegate)
		{
			return GameMode.GetGameSpecificValueInstaller(Mod, ModInstallLog, txFileManager, new NexusFileUtil(EnvironmentInfo), overwriteConfirmationDelegate);
		}

		#endregion

		/// <summary>
		/// Checks whether there's a log file for the current scripted installer.
		/// </summary>
		protected bool CheckScriptedModLog()
		{
			var modFilesPath = Path.Combine(Path.Combine(GameMode.GameModeEnvironmentInfo.InstallInfoDirectory, "Scripted"), Path.GetFileNameWithoutExtension(Mod.Filename)) + ".xml";

		    if (!string.IsNullOrWhiteSpace(ProfileManager?.IsScriptedLogPresent(Mod.Filename)))
            {
                return true;
            }

            return Directory.Exists(Path.Combine(GameMode.GameModeEnvironmentInfo.InstallInfoDirectory, "Scripted")) && File.Exists(modFilesPath);
		}

		/// <summary>
		/// Checks if there's an XML with the list of files to install for the current mod, if present the list of files will be returned.
		/// </summary>
		protected List<KeyValuePair<string, string>> LoadXMLModFilesToInstall()
		{
			string modFilesPath;
			if (ProfileManager != null)
            {
                modFilesPath =  ProfileManager.IsScriptedLogPresent(Mod.Filename) ?? Path.Combine(Path.Combine(GameMode.GameModeEnvironmentInfo.InstallInfoDirectory, "Scripted"), Path.GetFileNameWithoutExtension(Mod.Filename)) + ".xml";
            }
            else
            {
                modFilesPath = Path.Combine(Path.Combine(GameMode.GameModeEnvironmentInfo.InstallInfoDirectory, "Scripted"), Path.GetFileNameWithoutExtension(Mod.Filename)) + ".xml";
            }

            if (File.Exists(modFilesPath))
			{
				var scripted = XDocument.Load(modFilesPath);
				var files = new List<KeyValuePair<string, string>>();

				try
				{
					var xelFileList = scripted.Descendants("FileList").FirstOrDefault();
					if ((xelFileList != null) && xelFileList.HasElements)
					{
						foreach (var xelModFile in xelFileList.Elements("File"))
						{
							var strFileFrom = xelModFile.Attribute("FileFrom").Value;
							var strFileTo = xelModFile.Attribute("FileTo").Value;

						    if (!string.IsNullOrWhiteSpace(strFileFrom))
                            {
                                files.Add(new KeyValuePair<string, string>(strFileFrom, strFileTo));
                            }
                        }

						if (files.Count > 0)
                        {
                            return files;
                        }
                    }
				}
				catch (Exception e)
				{
				    var exceptionMessage = e.Message;

				    if (string.IsNullOrEmpty(exceptionMessage) && files.Count > 0)
				    {
				        return files;
				    }
				}
			}

			return null;
		}

		/// <summary>
		/// Registers the mod being installed with the install log.
		/// </summary>
		protected virtual void RegisterMod()
		{
			ModInstallLog.AddActiveMod(Mod);
		}
	}
}
