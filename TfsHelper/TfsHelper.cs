using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TfsHelperLib
{
    public class TfsHelper
    {
        DTE2 _appObj;
        #region Public Methods

        public TfsHelper(DTE2 appObj)
        {
            _appObj = appObj;
        }

        /// <summary>
        /// Gets the server Uri and the path of the selected item
        /// </summary>
        /// <param name="appObj">Application object</param>
        /// <param name="serverUri">Server Uri</param>
        /// <param name="itemPath">Item path</param>
        /// <param name="isFolder">True if the selected item is a folder</param>
        public void GetServerUriAndItemPath(out string serverUri, out string itemPath, out bool isFolder)
        {
            GetServerUriAndItemPath(_appObj, out serverUri, out itemPath, out isFolder);
        }

        /// <summary>
        /// Gets the server Uri and the path of the selected item
        /// </summary>
        /// <param name="appObj">Application object</param>
        /// <param name="serverUri">Server Uri</param>
        /// <param name="itemPath">Item path</param>
        /// <param name="isFolder">True if the selected item is a folder</param>
        public static void GetServerUriAndItemPath(DTE2 appObj, out string serverUri, out string itemPath, out bool isFolder)
        {
            if (appObj == null)
                throw new ArgumentNullException("appObj");

            isFolder = false;
            // Get local workspace info
            WorkspaceInfo[] wsInfo = Workstation.Current.GetAllLocalWorkspaceInfo();
            if (wsInfo.Length == 0)
                throw new TfsHistorySearchException("No workspace found.");

            // Get server Uri
            serverUri = wsInfo[0].ServerUri.AbsoluteUri;

            // Get a reference to the Team Foundation Server.
            TfsTeamProjectCollection tc = new TfsTeamProjectCollection(wsInfo[0].ServerUri);
            VersionControlServer vcs = tc.GetService(typeof(VersionControlServer)) as VersionControlServer;

            //if Active Window is Source Control Explorer
            if (appObj.ActiveWindow!= null && !String.IsNullOrWhiteSpace(appObj.ActiveWindow.Caption) 
                && appObj.ActiveWindow.Caption.StartsWith("Source Control Explorer"))
            {
                VersionControlExt vce;
                // The top level class used to access all other Team Foundation Version Control Extensiblity classes
                vce = appObj.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;

                if (!vce.Explorer.Connected)
                {
                    throw new TfsHistorySearchException("Source control explorer is not connected to a Team Foundation Server.");
                }

                // Get all selected items
                VersionControlExplorerItem[] selectedItems = vce.Explorer.SelectedItems;

                if (selectedItems.Length == 0)
                    throw new TfsHistorySearchException("You must select one item.");

                if (selectedItems.Length > 1)
                    throw new TfsHistorySearchException("Multiple items selected.");

                //Take the 1st item
                itemPath = selectedItems[0].SourceServerPath;
                isFolder = selectedItems[0].IsFolder;
            }
            //if Active Window is Solution Explorer
            else if (appObj.ActiveWindow != null && !String.IsNullOrWhiteSpace(appObj.ActiveWindow.Caption)
                && appObj.ActiveWindow.Caption.StartsWith("Solution Explorer"))
            {
                isFolder = false;
                if (appObj.SelectedItems.MultiSelect == true)
                    throw new TfsHistorySearchException("Multiple items selected.");

                SelectedItem selectedItem = appObj.SelectedItems.Item(1);

                SourceControl2 sc = (SourceControl2)appObj.SourceControl;

                if (selectedItem.ProjectItem != null)
                {
                    //Its a project file
                    string fileName = selectedItem.ProjectItem.Properties.Item("URL").Value.ToString();
                    fileName = Regex.Replace(fileName, "file:///", String.Empty, RegexOptions.IgnoreCase);
                    if (fileName.EndsWith("\\"))
                    {
                        fileName = fileName.Substring(0, fileName.LastIndexOf('\\'));
                        isFolder = true;
                    }
                    try
                    {
                        Item item = vcs.GetItem(fileName);
                        itemPath = item.ServerItem;
                    }
                    catch (Exception ex)
                    {
                        throw new TfsHistorySearchException("Item not under source control.", ex);
                    }
                }
                else if (selectedItem.Project != null)
                {
                    //Its a project
                    string fileName = selectedItem.Project.FileName;
                    try
                    {
                        Item item = vcs.GetItem(fileName);
                        itemPath = item.ServerItem;
                    }
                    catch (Exception ex)
                    {
                        throw new TfsHistorySearchException("Item not under source control.", ex);
                    }
                }
                else
                {
                    //check if .sln is selected 
                    if (appObj.ToolWindows != null && appObj.ToolWindows.SolutionExplorer != null && appObj.ToolWindows.SolutionExplorer.SelectedItems != null
                        && ((object[])appObj.ToolWindows.SolutionExplorer.SelectedItems).Length > 0
                        && ((EnvDTE.UIHierarchyItem)((object[])appObj.ToolWindows.SolutionExplorer.SelectedItems)[0]).Object is EnvDTE.Solution)
                    {
                        string fileName = appObj.SelectedItems.Item(1).DTE.Solution.FullName;
                        try
                        {
                            Item item = vcs.GetItem(fileName);
                            itemPath = item.ServerItem;
                        }
                        catch (Exception ex)
                        {
                            throw new TfsHistorySearchException("Item not under source control.", ex);
                        }
                    }
                    else
                    {
                        throw new TfsHistorySearchException("Operation is not supported for the selected item.");
                    }
                }
            }
            else
            {
                throw new TfsHistorySearchException("Operation is not supported.");
            }
        }

        /// <summary>
        /// Gets the changeset history of the specified item
        /// </summary>
        /// <param name="serverUri">Server Uri</param>
        /// <param name="itemPath">Item path</param>
        /// <returns>Changeset history</returns>
        public static List<TfsChangeset> GetHistory(string serverUri, string itemPath)
        {
            if (String.IsNullOrWhiteSpace(serverUri))
                throw new ArgumentException("'serverUri' is null or empty.");
            if (String.IsNullOrWhiteSpace(itemPath))
                throw new ArgumentException("'itemPath' is null or empty.");

            List<TfsChangeset> currentHistory = new List<TfsChangeset>();

            IEnumerable currentHistoryEnumerator = FetchHistory(serverUri, itemPath);
            if (currentHistoryEnumerator != null)
            {
                foreach (Changeset changeset in currentHistoryEnumerator)
                {
                    currentHistory.Add(new TfsChangeset() { 
                        ChangesetId = changeset.ChangesetId, Owner = changeset.Owner, 
                        CreationDate = changeset.CreationDate, Comment = changeset.Comment,
                        Changes_0_ServerItem = changeset.Changes[0].Item.ServerItem 
                    });
                }
            }
            return currentHistory;
        }

        /// <summary>
        /// Compares changesets
        /// </summary>
        /// <param name="sourceChangesetId">Source changeset Id</param>
        /// <param name="targetChangesetId">Target changeset Id</param>
        /// <param name="serverUrl">Server Uri</param>
        /// <param name="srcPath">Source item path</param>
        /// <param name="targetPath">Target item path</param>
        public static void Compare(string sourceChangesetId, string targetChangesetId, string serverUri, string srcPath, string targetPath)
        {
            if (String.IsNullOrWhiteSpace(sourceChangesetId))
                throw new ArgumentException("'sourceChangesetId' is null or empty.");
            if (String.IsNullOrWhiteSpace(targetChangesetId))
                throw new ArgumentException("'targetChangesetId' is null or empty.");
            if (String.IsNullOrWhiteSpace(serverUri))
                throw new ArgumentException("'serverUri' is null or empty.");
            if (String.IsNullOrWhiteSpace(srcPath))
                throw new ArgumentException("'srcPath' is null or empty.");
            if (String.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("'targetPath' is null or empty.");

            TfsTeamProjectCollection tc = new TfsTeamProjectCollection(new Uri(serverUri));
            VersionControlServer vcs = tc.GetService(typeof(VersionControlServer)) as VersionControlServer;

            VersionSpec sourceVersion = VersionSpec.ParseSingleSpec(sourceChangesetId, vcs.AuthorizedUser);
            VersionSpec targetVersion = VersionSpec.ParseSingleSpec(targetChangesetId, vcs.AuthorizedUser);

            Difference.VisualDiffItems(vcs,
                                       Difference.CreateTargetDiffItem(vcs, srcPath, sourceVersion, 0, sourceVersion),
                                       Difference.CreateTargetDiffItem(vcs, targetPath, targetVersion, 0, targetVersion));
        }

        /// <summary>
        /// Compares changesets
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="sourceChangesetId">Source changeset Id</param>
        /// <param name="serverUrl">Server Uri</param>
        /// <param name="srcPath">Source item path</param>
        public static void CompareLocal(string localPath, string sourceChangesetId, string serverUri, string srcPath)
        {
            if (String.IsNullOrWhiteSpace(sourceChangesetId))
                throw new ArgumentException("'sourceChangesetId' is null or empty.");
            if (String.IsNullOrWhiteSpace(serverUri))
                throw new TfsHistorySearchException("'serverUri' is null or empty.");
            if (String.IsNullOrWhiteSpace(srcPath))
                throw new TfsHistorySearchException("'srcPath' is null or empty.");
            if (String.IsNullOrWhiteSpace(localPath))
                throw new TfsHistorySearchException("'localPath' is null or empty.");

            TfsTeamProjectCollection tc = new TfsTeamProjectCollection(new Uri(serverUri));
            VersionControlServer vcs = tc.GetService(typeof(VersionControlServer)) as VersionControlServer;

            //VersionSpec sourceVersion = VersionSpec.ParseSingleSpec(sourceChangesetId, vcs.TeamFoundationServer.AuthenticatedUserName);
            VersionSpec sourceVersion = VersionSpec.ParseSingleSpec(sourceChangesetId, vcs.AuthorizedUser);

            //VersionSpec targetVersion = VersionSpec.ParseSingleSpec(targetChangesetId, vcs.TeamFoundationServer.AuthenticatedUserName);

            //Difference.DiffFiles(
            Difference.VisualDiffItems(vcs, Difference.CreateTargetDiffItem(vcs, srcPath, sourceVersion, 0, sourceVersion), Difference.CreateTargetDiffItem(vcs, localPath, null, 0, null));
            //Difference.VisualDiffFiles();
            //Difference.VisualDiffItems(vcs,
            //                           Difference.CreateTargetDiffItem(vcs, srcPath, sourceVersion, 0, sourceVersion),
            //                           Difference.CreateTargetDiffItem(vcs, targetPath, targetVersion, 0, targetVersion));
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Retrieves an enumerable collection of changesets matching the specified items
        /// </summary>
        /// <param name="serverUri">server Uri</param>
        /// <param name="itemPath">Item path</param>
        /// <returns>An enumerable collection of changesets</returns>
        private static IEnumerable FetchHistory(string serverUri, string itemPath)
        {
            IEnumerable enumerable1 = null;
            if (String.IsNullOrWhiteSpace(serverUri))
                throw new TfsHistorySearchException("'serverUri' is null or empty.");
            if (String.IsNullOrWhiteSpace(itemPath))
                throw new TfsHistorySearchException("'itemPath' is null or empty.");

            TfsTeamProjectCollection tc = new TfsTeamProjectCollection(new Uri(serverUri));

            VersionControlServer vcs = tc.GetService(typeof(VersionControlServer)) as VersionControlServer;
            ExtendedItem[] itemArray1 = vcs.GetExtendedItems(itemPath, DeletedState.NonDeleted, ItemType.Any);

            if ((itemArray1 == null) || (itemArray1.Length == 0))
                throw new TfsHistorySearchException("Specified item not found.");

            ExtendedItem item1 = itemArray1[0];
            enumerable1 = vcs.QueryHistory(itemPath, VersionSpec.Latest, item1.DeletionId, (item1.ItemType == ItemType.Folder) ? RecursionType.Full : RecursionType.None, "", new ChangesetVersionSpec(1), VersionSpec.Latest, 0x7fffffff, true, false);

            return enumerable1;
        }
        #endregion
    }
}
