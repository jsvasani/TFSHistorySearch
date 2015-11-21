using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TfsHelperLib;

namespace TfsHistorySearchUI
{
    public partial class TfsHistorySearchWinForm : Form
    {
        #region Private Members
        private string _itemPath = String.Empty;
        private string _serverUri = String.Empty;
        private bool _isFolder;
        List<TfsChangeset> _currentHistory = new List<TfsChangeset>();
        List<TfsChangeset> _currentResult = new List<TfsChangeset>();
        
        #endregion

        #region Public Methods 

        public TfsHistorySearchWinForm(TfsHelper tfsHelper)
        {
            InitializeComponent();

            //Get the server Uri and server path of the selected item 
            tfsHelper.GetServerUriAndItemPath(out this._serverUri, out this._itemPath, out _isFolder);
            textBoxServerName.Text = this._serverUri;
            textBoxFilePath.Text = this._itemPath;
        }

        #endregion 

        #region Event Handlers

        /// <summary>
        /// Searches the item history for the specified keywords
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSearch_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel.Text = "Searching...";
            this.listViewSearchResults.Items.Clear();
            this._currentResult.Clear();
            Cursor.Current = Cursors.WaitCursor;
            _serverUri = textBoxServerName.Text;
            _itemPath = textBoxFilePath.Text;

            try
            {
                // Get all history of specified item
                this._currentHistory = TfsHelper.GetHistory(_serverUri, _itemPath);

                if (_currentHistory.Count > 0)
                {
                    string[] searchWords = Regex.Split(textBoxSearch.Text, "\\s* |,");
                    _currentResult = SearchHistory(this._currentHistory, searchWords);
                    if (_currentResult.Count > 0)
                    {
                        PopulateListView(listViewSearchResults, _currentResult);
                    }
                    toolStripStatusLabel.Text = _currentResult.Count.ToString() + " results found";
                }
                else
                {
                    toolStripStatusLabel.Text = "0 results found";
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = string.Empty;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Controls the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewSearchResults_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            //Compare with latest version
            contextMenuResults.Items[0].Enabled = false;
            //Compare with previous version
            contextMenuResults.Items[1].Enabled = false;
            //Compare
            contextMenuResults.Items[2].Enabled = false;

            // If not folder then only enable options 
            if (!_isFolder)
            {
                if (listViewSearchResults.SelectedItems.Count == 1)
                {
                    //Compare with latest version
                    contextMenuResults.Items[0].Enabled = true;
                    //Compare with previous version
                    contextMenuResults.Items[1].Enabled = true;
                }
                else if (listViewSearchResults.SelectedItems.Count == 2)
                {
                    //Compare
                    contextMenuResults.Items[2].Enabled = true;
                }
            }
        }

        /// <summary>
        /// Compares the selected changeset with previous version  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compareWithPreviousVersionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Index in result
                int firstIndex = this.listViewSearchResults.SelectedIndices[0];
                //Index in actual history
                firstIndex = _currentHistory.IndexOf(this._currentResult[firstIndex]);
                int secondIndex = firstIndex + 1;

                if (secondIndex < _currentHistory.Count)
                {
                    string sourceChangesetId = this._currentHistory[firstIndex].ChangesetId.ToString();
                    string targetChangesetId = this._currentHistory[secondIndex].ChangesetId.ToString();
                    //srcPath and targetPath will be different in case of renamed else same
                    string srcPath = this._currentHistory[firstIndex].Changes_0_ServerItem;
                    string targetPath = this._currentHistory[secondIndex].Changes_0_ServerItem;

                    TfsHelper.Compare(sourceChangesetId, targetChangesetId, _serverUri, srcPath, targetPath);
                }
                else
                {
                    MessageBox.Show("No previous version available.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Compares the selected changesets
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void compareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                int firstIndex = this.listViewSearchResults.SelectedIndices[0];
                int secondIndex = this.listViewSearchResults.SelectedIndices[1];

                string sourceChangesetId = this._currentResult[firstIndex].ChangesetId.ToString();
                string targetChangesetId = this._currentResult[secondIndex].ChangesetId.ToString();
                //srcPath and targetPath will be different in case of renamed else same
                string srcPath = this._currentResult[firstIndex].Changes_0_ServerItem;
                string targetPath = this._currentResult[secondIndex].Changes_0_ServerItem;

                TfsHelper.Compare(sourceChangesetId, targetChangesetId, _serverUri, srcPath, targetPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void compareWithLatestVersionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Index in result
                int firstIndex = this.listViewSearchResults.SelectedIndices[0];
                //Index in actual history
                firstIndex = _currentHistory.IndexOf(this._currentResult[firstIndex]);
                int secondIndex = 0;

                string sourceChangesetId = this._currentHistory[firstIndex].ChangesetId.ToString();
                string targetChangesetId = this._currentHistory[secondIndex].ChangesetId.ToString();
                //srcPath and targetPath will be different in case of renamed else same
                string srcPath = this._currentHistory[firstIndex].Changes_0_ServerItem;
                string targetPath = this._currentHistory[secondIndex].Changes_0_ServerItem;

                TfsHelper.Compare(sourceChangesetId, targetChangesetId, _serverUri, srcPath, targetPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion 

        #region Private Methods

        /// <summary>
        /// Gets the changesets matching the search criteria
        /// </summary>
        /// <param name="itemHistoryChangesets">Changesets to be searched on</param>
        /// <param name="searchWords">Search words</param>
        /// <returns></returns>
        private static List<TfsChangeset> SearchHistory(List<TfsChangeset> itemHistoryChangesets, string[] searchWords)
        {
            List<TfsChangeset> results = new List<TfsChangeset>();

            foreach (TfsChangeset changeset in itemHistoryChangesets)
            {
                // Prepare the string for free text search
                string historyRow = changeset.ChangesetId.ToString()
                    + " " + changeset.Owner
                    + " " + changeset.CreationDate.ToLongDateString()
                    + " " + changeset.Comment;

                //Check if all the words are present
                bool allKeywordsPresent = true;
                foreach (string word in searchWords)
                {
                    if (historyRow.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        allKeywordsPresent = false;
                        break;
                    }
                }
                if (allKeywordsPresent == true)
                {
                    results.Add(changeset);
                }
            }
            return results;
        }

        private static void PopulateListView(ListView listView, List<TfsChangeset> changesets)
        {
            listView.Items.Clear();
            foreach (var changeset in changesets)
            {
                string comment = String.Empty;
                //If Comment present then replace
                if (!String.IsNullOrEmpty(changeset.Comment))
                {
                    comment = changeset.Comment.Replace("\n", " ");
                    comment = comment.Replace("\r", String.Empty);
                }

                string[] fields = new string[] {
                                        changeset.ChangesetId.ToString(),
                                        changeset.Owner, 
                                        changeset.CreationDate.ToString(), 
                                        comment
                                        };
                ListViewItem lvi = new ListViewItem(fields);
                listView.Items.Add(lvi);
            }
        }

        #endregion 
    }
}
