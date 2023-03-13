﻿using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Data.SqlClient;

namespace Project_Zero
{
    public partial class Main_UI : Form
    {
        public Main_UI()
        {
            InitializeComponent();
        }

        // SET: Title Bar 'DARK MODE'
        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);


        SqlConnection conn; SqlDataAdapter dataAdapter; DataSet dataSet; SqlCommandBuilder commandBuilder;

        string database = "data.mdf", table = "Series", tempName = "", devURL = "https://www.github.com/pahasara/zero";

        int maxRows = 0, currentRow = 0, progressBarValue, progressBarMaxLength, progressForwardCount; 
        
        bool isProgressReset = false; int rating, delayTime;


        private void Main_UI_Load(object sender, EventArgs e)
        {
            setProgressBar();
            setToolTips();
            createConnection();
            navigateRecords();
        }


        private void createConnection()
        {
            try
            {
                conn = new SqlConnection();
                dataSet = new DataSet();
                conn.ConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\" + database + ";Integrated Security=True";
                string sql = "SELECT * FROM " + table;
                dataAdapter = new SqlDataAdapter(sql, conn);
                dataAdapter.Fill(dataSet, "Series");
                commandBuilder = new SqlCommandBuilder(dataAdapter);
                maxRows = dataSet.Tables["Series"].Rows.Count;
            }
            catch (Exception)
            {
                errorEmptyDatabase();
                showMessage("dbLost");
                Application.Exit();
            }
        }

        private void navigateRecords()
        {
            try
            {
                DataRow dataRow = dataSet.Tables["Series"].Rows[currentRow];
                getRecord(dataRow);
                setTempName();
                setJumpingButtons();
                showProgress();
                showInfo(maxRows + " series");
                setWatchingStatus();
                setRating();
                progressBar.Focus();
            }
            catch (Exception)
            {
                errorEmptyDatabase();
            }
        }

        private void getRecord(DataRow dataRow)
        {
            txtName.Text = dataRow.ItemArray.GetValue(0).ToString();
            txtSeries.Text = dataRow.ItemArray.GetValue(1).ToString();
            cBoxStatus.Text = dataRow.ItemArray.GetValue(2).ToString();
            txtWatched.Text = dataRow.ItemArray.GetValue(3).ToString();
            txtEpisodes.Text = dataRow.ItemArray.GetValue(4).ToString();
            int.TryParse(dataRow.ItemArray.GetValue(5).ToString(), out rating);
        }

        private void setRecord(DataRow newRow)
        {
            int watched, episodes;
            int.TryParse(txtWatched.Text, out watched);
            int.TryParse(txtEpisodes.Text, out episodes);

            if (episodes == 0) { episodes = 1; }
            if (watched > episodes) { watched = episodes; }
            newRow[0] = txtName.Text;
            newRow[1] = txtSeries.Text;
            newRow[2] = cBoxStatus.Text;
            newRow[3] = watched.ToString();
            newRow[4] = episodes.ToString();
            newRow[5] = rating.ToString();
        }

        private void updateRecord(bool isRatingUpdate = false)
        {
            
            DataRow dataRow = dataSet.Tables["Series"].Rows[currentRow];

            setWatchingStatus();
            setRecord(dataRow);
            dataAdapter.Update(dataSet, "Series");

            if (!isRatingUpdate) 
            { 
                navigateRecords();
                showInfo("Series updated");
            }
            else
            {
                setRating();
            }
        }

        private void insertRecord()
        {
            DataRow newRow = dataSet.Tables["Series"].NewRow();
            setRecord(newRow);

            dataSet.Tables["Series"].Rows.Add(newRow);
            dataAdapter.Update(dataSet, "Series");

            btnNew.Text = "NEW";
            btnUpdate.Text = "UPDATE";
            changeSituation();

            maxRows++;
            currentRow = maxRows - 1;
            navigateRecords();
            showInfo("Added new series");
        }

        private void deleteRecord()
        {
            dataSet.Tables["Series"].Rows[currentRow].Delete();
            dataAdapter.Update(dataSet, "Series");

            setTempName("");
            maxRows--;
            if (currentRow > 0) { getPreviousRow(); }
            else { navigateRecords(); }
            showInfo("Series deleted");
        }

        private void searchRecord()
        {
            try
            {
                string searchFor = txtName.Text;
                DataRow[] returnedRows = dataSet.Tables["Series"].Select("Name='" + searchFor + "'");
                int numberOfResults = returnedRows.Length;

                if (numberOfResults > 0)
                {
                    DataRow dataRow = returnedRows[0];
                    getRecord(dataRow);

                    currentRow = dataSet.Tables["Series"].Rows.IndexOf(dataRow);
                    setJumpingButtons();
                    showInfo("Search success");
                }
                else
                {
                    txtSeries.Clear();
                    txtWatched.Clear();
                    txtEpisodes.Clear();
                    resetRating();
                    btnNew.Text = "BACK";
                    changeSituation();
                    showProgress();
                    showInfo("No matching");
                }
            }
            catch (Exception)
            {
                showInfo("Invalid search", "Error");
            }
        }

        private void progressForward()
        {
            try
            {
                isProgressReset = false;
                int currentEpisode = Convert.ToInt32(txtWatched.Text);
                int numberOfEpisodes = Convert.ToInt32(txtEpisodes.Text);
                currentEpisode++;
                txtWatched.Text = currentEpisode.ToString();
                updateRecord();
                if (currentEpisode < numberOfEpisodes)
                {
                    progressForwardCount++;
                    showInfo("Progress +" + progressForwardCount);
                }
                else
                {
                    setWatchingStatus();
                }
            }
            catch (Exception)
            {
                showInfo("Update failed", "Error");
            }
        }

        private void showProgress()
        {
            try
            {
                Double lastWatchedEpisode = Double.Parse(txtWatched.Text);
                Double numberOfEpisodes = Double.Parse(txtEpisodes.Text);
                Double dPercentage = lastWatchedEpisode / numberOfEpisodes;

                int iPercentage = (int)(dPercentage * 100);
                progressBarValue = (int)(dPercentage * progressBarMaxLength);

                progressCorner.Width = 2;
                setProgressCorner();

                progressTimer.Start();
                txtProgress.Text = iPercentage.ToString() + "%";
                if (iPercentage == 0)
                {
                    progressCorner.Width = 0;
                }
            }
            catch (Exception)
            {
                progressBar.Width = 0;
                progressCorner.Width = 0;
                txtProgress.Text = "0%";
            }
        }

        private void getNextRow()
        {
            resetProgressBar();
            currentRow++;
            navigateRecords();
        }

        private void getPreviousRow()
        {
            resetProgressBar();
            currentRow--;
            navigateRecords();
        }

        private void setRating()
        {
            PictureBox[] star = new PictureBox[5];
            star[0] = star1;
            star[1] = star2;
            star[2] = star3;
            star[3] = star4;
            star[4] = star5;

            try
            {
                for (int i = 0; i < rating; i++)
                {
                    star[i].Image = Project_Zero.Properties.Resources.btnStar;
                }

                for (int i = 4; i >= rating; i--)
                {
                    star[i].Image = Project_Zero.Properties.Resources.btnStar_default;
                }
            }
		catch (Exception)
            {
                resetRating();
                updateRating();
            }
        }

        private void disableRating()
        {
            disableControl(star1);
            disableControl(star2);
            disableControl(star3);
            disableControl(star4);
            disableControl(star5);
        }

        private void enableRating()
        {
            enableControl(star1);
            enableControl(star2);
            enableControl(star3);
            enableControl(star4);
            enableControl(star5);
        }

        private void updateRating()
        {
            updateRecord(true);
        }

        private void resetRating()
        {
            rating = 0;
            setRating();
        }

        private void showMessage(string mode)
        {
            Message_UI msgForm = new Message_UI();
            msgForm.mode = mode;
            msgForm.ShowDialog();
            bool confirm = msgForm.isYesClicked;
            msgForm.Dispose();

            if (confirm)
            {
                if (mode == "delete") deleteRecord();
                if (mode == "reset") resetWatchingProgress();
                if (mode == "dbLost") Process.Start(devURL);
                if (mode == "finish") completeSeries();
            }
        }

        private void errorEmptyDatabase()
        {
            txtName.Clear();
            txtSeries.Clear();
            resetRating();

            txtWatched.Text = "0";
            txtEpisodes.Text = "12";
            btnNew.Text = "CANCEL";
            btnUpdate.Text = "SAVE";

            disableControl(btnNew);
            changeSituation();

            showProgress();
            showInfo("Empty database", "Error");
        }

        private void showInfo(string infoText, string infoTitle = "Info")
        {
            labelST.Text = infoTitle + " |  " + infoText;
        }

        private void setTempName(string temp = " ")
        {
            if (temp == " ")
            {
                tempName = txtName.Text;
            }
            else
            {
                tempName = temp;
            }
        }

        private void setToolTips()
        {
            string starMark = "*required";

            tool_tip.OwnerDraw = true;
            tool_tip.BackColor = Color.FromArgb(28, 28, 28);
            tool_tip.ForeColor = Color.FromArgb(255, 45, 45);

            tool_tip.SetToolTip(btnReset, "Reset progress");
            tool_tip.SetToolTip(btnSearch, "Search series");
            tool_tip.SetToolTip(btnPlus, "Progress +1");
            tool_tip.SetToolTip(btnNext, "Next series");
            tool_tip.SetToolTip(btnBack, "Previous series");
            tool_tip.SetToolTip(starName, starMark);
            tool_tip.SetToolTip(starWatched, starMark);
            tool_tip.SetToolTip(starEpisodes, starMark);
            tool_tip.SetToolTip(cBoxStatus, "Series status");

        }

        private bool checkInputFields()
        {
            if (txtName.Text != "")
            {
                if (txtWatched.Text != "")
                {
                    if (txtEpisodes.Text != "")
                    {
                        return true;
                    }
                    else
                    {
                        showInfo("Empty field", "Error");
                    }
                }
                else
                {
                    showInfo("Empty field", "Error");
                }
            }
            else
            {
                showInfo("Empty name", "Error");
            }
            return false;
        }

        private void changeSituation()
        {
            if (btnUpdate.Text == "SAVE")
            {
                disableControl(btnSearch);
                disableControl(btnDelete);
                disableControl(btnNext);
                disableControl(btnBack);
                disableControl(btnPlus);
                disableControl(cBoxStatus);
                hideControl(btnReset);
                disableRating();
                cBoxStatus.Checked = false;
            }
            else if (btnNew.Text == "BACK")
            {
                disableControl(btnDelete);
                disableControl(btnNext);
                disableControl(btnBack);
                disableControl(btnPlus);
                disableControl(btnUpdate);
                disableControl(cBoxStatus);
                hideControl(btnReset);
                disableRating();
                cBoxStatus.Checked = false;
            }
            else
            {
                enableControl(btnSearch);
                enableControl(btnNew);
                enableControl(btnPlus);
                enableControl(btnUpdate);
                enableControl(btnDelete);
                enableControl(cBoxStatus);
                showControl(btnReset);
                enableRating();
            }
            setButtonShadows();
        }

        private void setJumpingButtons()
        {
            if (currentRow == 0)
            {
                disableControl(btnBack);
                if (maxRows > 1)
                {
                    enableControl(btnNext);
                }
                else
                {
                    disableControl(btnNext);
                }
            }
            else if (currentRow < (maxRows - 1))
            {
                enableControl(btnNext);
                if (currentRow == (maxRows - 1))
                {
                    disableControl(btnBack);
                }
                else
                {
                    enableControl(btnBack);
                }
            }
            else
            {
                disableControl(btnNext);
                if (currentRow > 0)
                {
                    enableControl(btnBack);
                }
            }
            setButtonShadows();
        }

        private void setButtonShadows()
        {
            if (btnNext.Enabled)
            {
                showControl(btnNextShadow);
            }
            else
            {
                hideControl(btnNextShadow);
            }

            if (btnBack.Enabled)
            {
                showControl(btnBackShadow);
            }
            else
            {
                hideControl(btnBackShadow);
            }

            if (btnNew.Enabled)
            {
                showControl(btnAddShadow);
            }
            else
            {
                hideControl(btnAddShadow);
            }

            if (btnUpdate.Enabled)
            {
                showControl(btnUpdateShadow);
            }
            else
            {
                hideControl(btnUpdateShadow);
            }

            if (btnDelete.Enabled)
            {
                showControl(btnDeleteShadow);
            }
            else
            {
                hideControl(btnDeleteShadow);
            }

            if (btnSearch.Enabled)
            {
                showControl(btnSearchShadow);
            }
            else
            {
                hideControl(btnSearchShadow);
            }
        }

        private void enableControl(Control button)
        {
            button.Enabled = true;
        }

        private void disableControl(Control button)
        {
            button.Enabled = false;
        }

        private void showControl(Control shadow)
        {
            shadow.Visible = true;
        }

        private void hideControl(Control shadow)
        {
            shadow.Visible = false;
        }

        private void isNumeric(KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
        }

        private void setProgressBar()
        {
            progressBarMaxLength = progressOut.Width;
            progressBar.Width = 0;
        }

        private void resetProgressBar()
        {
            progressTimer.Stop();
            progressBar.Width = 0;
            setProgressCorner();
        }

        private void setProgressCorner()
        {
            progressCorner.Left = (progressBar.Right - 1);
        }

        private void completeSeries()
        {
            txtWatched.Text = txtEpisodes.Text;
            cBoxStatus.Text = "Completed";
            updateRecord();
        }

        private void setWatchingStatus()
        {
            int watched, episodes;
            int.TryParse(txtWatched.Text, out watched);
            int.TryParse(txtEpisodes.Text, out episodes);

            if (watched == episodes)
            {
                cBoxStatus.Text = "Completed";
                showInfo("Series finished");
            }
            else
            {
                cBoxStatus.Text = "Watching";
            }
            if (cBoxStatus.Text == "Completed")
            {
                cBoxStatus.Checked = true;
                disableControl(btnPlus);
            }
            else
            {
                cBoxStatus.Checked = false;
                enableControl(btnPlus);
            }
        }

        private void resetWatchingProgress()
        {
            txtWatched.Text = "0";
            updateRecord();
            showInfo("Progress resetted");
        }

        private void seriesEnded()
        {
            
            if (cBoxStatus.Text != "Completed")
            {
                cBoxStatus.Checked = false;
                showMessage("finish");
            }
            else
            {
                //txtWatched.Text = txtEpisodes.Text;
                setWatchingStatus();
                showInfo("Series finished");
            }
        }



        private void progressTimer_Tick(object sender, EventArgs e)
        {
            if (isProgressReset)
            {
                progressForwardCount = 0;
                progressBar.Width = 0;
                setProgressCorner();
                isProgressReset = false;
            }
            else
            {
                setProgressCorner();
            }

            if (progressBar.Width < progressBarValue)
            {
                progressBar.Width += 1;
                setProgressCorner();
            }
            else
            {
                if (progressBarValue == progressBarMaxLength)
                {
                    progressCorner.Width = 0;
                }
                isProgressReset = true;
                progressTimer.Stop();
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            getPreviousRow();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            getNextRow();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if(btnNew.Text == "NEW")
            {
                resetProgressBar();
                showInfo("Adding series");
                txtName.Clear();
                txtSeries.Clear();
                resetRating();
                setTempName();
                txtWatched.Text = "0";
                txtEpisodes.Text = "12";
                btnNew.Text = "CANCEL";
                btnUpdate.Text = "SAVE";
                showProgress();
            }
            else if (btnNew.Text == "BACK")
            {
                btnNew.Text = "NEW";
                navigateRecords();
            }
            else
            {
                enableControl(btnPlus);
                setJumpingButtons();
                btnNew.Text = "NEW";
                btnUpdate.Text = "UPDATE";
                navigateRecords();
            }
            changeSituation();
            progressBar.Focus();
        }

        private void txtStatus_CheckedChanged(object sender, EventArgs e)
        {
            if(cBoxStatus.Checked)
            {
                seriesEnded();
                
            }
            else
            {
                cBoxStatus.Text = "Watching";          
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if(btnUpdate.Text == "UPDATE")
            {
                if (checkInputFields())
                {
                    updateRecord();
                    
                }
            }
            else
            {
                if (checkInputFields())
                {
                    insertRecord();
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            searchRecord();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            showMessage("delete");
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            if (DwmSetWindowAttribute(Handle, 19, new[] { 1 }, 4) != 0)
                DwmSetWindowAttribute(Handle, 20, new[] { 1 }, 4);
        }

        private void txtName_Click(object sender, EventArgs e)
        {
            if (tempName == txtName.Text)
            {
                txtName.Clear();
            }
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                searchRecord();
            }
            else if (e.KeyCode == Keys.Back)
            {
                if (btnNew.Text == "BACK")
                {
                    btnNew.Text = "NEW";
                    changeSituation();
                    navigateRecords();
                }
            }
        }

        private void txtName_Enter(object sender, EventArgs e)
        {
            showControl(txtNameBack);
            txtName.ForeColor = Color.FromArgb(255, 230, 230);
        }

        private void txtName_Leave(object sender, EventArgs e)
        {
            if (txtName.Text == "")
            {
                txtName.Text = tempName;
            }
            else if (tempName != txtName.Text)
            {
                setTempName();
            }
            hideControl(txtNameBack);
            txtName.ForeColor = Color.FromArgb(255, 210, 210);
        }

        private void txtWatched_Enter(object sender, EventArgs e)
        {
            showControl(txtWatchedBack);
            txtWatched.ForeColor = Color.FromArgb(255, 230, 230);
        }

        private void txtWatched_Leave(object sender, EventArgs e)
        {
            hideControl(txtWatchedBack);
            txtWatched.ForeColor = Color.FromArgb(255, 210, 210);
        }

        private void txtEpisodes_Enter(object sender, EventArgs e)
        {
            showControl(txtEpisodesBack);
            txtEpisodes.ForeColor = Color.FromArgb(255, 230, 230);
        }

        private void txtEpisodes_Leave(object sender, EventArgs e)
        {
            hideControl(txtEpisodesBack);
            txtEpisodes.ForeColor = Color.FromArgb(255, 210, 210);
        }

        private void txtSeries_Enter(object sender, EventArgs e)
        {
            showControl(txtSeriesBack);
            txtSeries.ForeColor = Color.FromArgb(255, 230, 230);
        }

        private void txtSeries_Leave(object sender, EventArgs e)
        {
            hideControl(txtSeriesBack);
            txtSeries.ForeColor = Color.FromArgb(255, 210, 210);
        }

        private void btnPlus_MouseDown(object sender, MouseEventArgs e)
        {
            btnPlus.ForeColor = Color.FromArgb(100, 0, 0);
        }

        private void btnPlus_Click(object sender, EventArgs e)
        {
            progressForward();
        }
        
        private void btnPlus_MouseMove(object sender, MouseEventArgs e)
        {
            btnPlus.ForeColor = Color.FromArgb(255, 37, 40);
        }

        private void btnPlus_MouseLeave(object sender, EventArgs e)
        {
            btnPlus.ForeColor = Color.FromArgb(80,80,80);
        }

        private void btnReset_MouseMove(object sender, MouseEventArgs e)
        {
            btnReset.Image = Project_Zero.Properties.Resources.btn_Reset_move;
        }

        private void btnReset_MouseLeave(object sender, EventArgs e)
        {
            btnReset.Image = Project_Zero.Properties.Resources.btn_Reset;
        }

        private void btnReset_MouseDown(object sender, MouseEventArgs e)
        {
            btnReset.Image = Project_Zero.Properties.Resources.btn_Reset_down;
        }

        private void star1_Click(object sender, EventArgs e)
        {
            rating = 1;
            updateRating();
        }

        private void star2_Click(object sender, EventArgs e)
        {
            rating = 2;
            updateRating();
        }

        private void star3_Click(object sender, EventArgs e)
        {
            rating = 3;
            updateRating();
        }

        private void star4_Click(object sender, EventArgs e)
        {
            rating = 4;
            updateRating();
        }

        private void star5_Click(object sender, EventArgs e)
        {
            rating = 5;
            updateRating();
        }

        private void MUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            conn.Close();
            Application.Exit();
        }

        private void MUID_Deactivate(object sender, EventArgs e)
        {
            progressBar.Focus();
        }

        private void txtRating_KeyPress(object sender, KeyPressEventArgs e)
        {
            isNumeric(e);

            // Only one decimal allowed
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

        private void txtWatched_KeyPress(object sender, KeyPressEventArgs e)
        {
            isNumeric(e);
        }

        private void txtEpisodes_KeyPress(object sender, KeyPressEventArgs e)
        {
            isNumeric(e);
        }
        
        private void tool_tip_Draw(object sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.DrawText();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            showMessage("reset");
        }
    }
}