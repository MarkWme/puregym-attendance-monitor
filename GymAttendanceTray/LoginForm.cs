using System.Windows.Forms;

namespace GymAttendanceTray;

public partial class LoginForm : Form
{
    private TextBox emailTextBox = null!;
    private TextBox pinTextBox = null!;
    private CheckBox saveCredentialsCheckBox = null!;
    private Button okButton = null!;
    private Button cancelButton = null!;

    public string Email => emailTextBox.Text;
    public string Pin => pinTextBox.Text;
    public bool SaveCredentials => saveCredentialsCheckBox.Checked;

    public LoginForm()
    {
        InitializeComponent();
        LoadSavedCredentials();
    }

    private void InitializeComponent()
    {
        this.Text = "PureGym Login";
        this.Size = new Size(400, 250);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // Email label and textbox
        var emailLabel = new Label
        {
            Text = "Email:",
            Location = new Point(20, 20),
            Size = new Size(80, 23)
        };
        this.Controls.Add(emailLabel);

        emailTextBox = new TextBox
        {
            Location = new Point(110, 20),
            Size = new Size(250, 23),
            TabIndex = 0
        };
        this.Controls.Add(emailTextBox);

        // PIN label and textbox
        var pinLabel = new Label
        {
            Text = "PIN:",
            Location = new Point(20, 60),
            Size = new Size(80, 23)
        };
        this.Controls.Add(pinLabel);

        pinTextBox = new TextBox
        {
            Location = new Point(110, 60),
            Size = new Size(250, 23),
            UseSystemPasswordChar = true,
            TabIndex = 1
        };
        this.Controls.Add(pinTextBox);

        // Save credentials checkbox
        saveCredentialsCheckBox = new CheckBox
        {
            Text = "Save credentials securely",
            Location = new Point(110, 100),
            Size = new Size(200, 23),
            TabIndex = 2
        };
        this.Controls.Add(saveCredentialsCheckBox);

        // Info label
        var infoLabel = new Label
        {
            Text = "Credentials will be stored securely in Windows Credential Manager",
            Location = new Point(110, 130),
            Size = new Size(250, 40),
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, 8)
        };
        this.Controls.Add(infoLabel);

        // OK button
        okButton = new Button
        {
            Text = "OK",
            Location = new Point(200, 180),
            Size = new Size(75, 23),
            TabIndex = 3,
            DialogResult = DialogResult.OK
        };
        okButton.Click += OkButton_Click;
        this.Controls.Add(okButton);

        // Cancel button
        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(285, 180),
            Size = new Size(75, 23),
            TabIndex = 4,
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(cancelButton);

        this.AcceptButton = okButton;
        this.CancelButton = cancelButton;
    }

    private void LoadSavedCredentials()
    {
        try
        {
            var credentials = CredentialManager.ReadCredential("PureGym_Monitor");
            if (credentials != null)
            {
                emailTextBox.Text = credentials.UserName ?? "";
                pinTextBox.Text = credentials.Password ?? "";
                saveCredentialsCheckBox.Checked = true;
            }
        }
        catch
        {
            // Ignore errors loading credentials
        }
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(emailTextBox.Text) || string.IsNullOrWhiteSpace(pinTextBox.Text))
        {
            MessageBox.Show("Please enter both email and PIN.", "Required Fields",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (saveCredentialsCheckBox.Checked)
        {
            try
            {
                CredentialManager.WriteCredential("PureGym_Monitor", emailTextBox.Text, pinTextBox.Text,
                                                "PureGym Monitor Credentials");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save credentials: {ex.Message}", "Warning",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            // Remove saved credentials if unchecked
            try
            {
                CredentialManager.DeleteCredential("PureGym_Monitor");
            }
            catch
            {
                // Ignore errors deleting credentials
            }
        }

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}