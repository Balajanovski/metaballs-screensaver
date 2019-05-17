Imports Microsoft.Win32
Imports System.Windows.Forms

Public Class ConfigurationWindow

    Private savedSettings As Boolean = False

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        savedSettings = False
    End Sub

    Private Sub SaveSettings()
        savedSettings = True

        ' Create or get existing Registry subkey
        Dim key As RegistryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Metaballs_ScreenSaver")

        key.SetValue("waveheight", waveheightSlider.Value)
        key.SetValue("speed", speedSlider.Value)
        key.SetValue("dropletNumber", dropletNumberSlider.Value)

        MessageBox.Show("Screensaver Settings Saved Successfully!", "Settings Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1)
    End Sub

    Private Sub Window_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs)
        If Not savedSettings Then
            Dim result As DialogResult = MessageBox.Show("You are about to exit without saving" _
                & vbCrLf & "Are you sure you want to proceed",
                                                      "Exit Without Saving?",
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Question,
                                                      MessageBoxDefaultButton.Button1)

            If result = Windows.Forms.DialogResult.Yes Then
                e.Cancel = False
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub SaveButton_Click(sender As Object, e As Windows.RoutedEventArgs) Handles saveButton.Click
        SaveSettings()
    End Sub

    Private Sub SpeedSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles speedSlider.ValueChanged
        savedSettings = False

        speedLabel.Content = "Speed (Value: " & String.Format("{0:0.00}", speedSlider.Value) & "):"
    End Sub

    Private Sub DropletNumberSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles dropletNumberSlider.ValueChanged
        savedSettings = False

        dropletNumberLabel.Content = "Droplet Number (Value: " & String.Format("{0:0}", dropletNumberSlider.Value) & "):"
    End Sub

    Private Sub WaveheightSlider_ValueChanged(sender As Object, e As Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles waveheightSlider.ValueChanged
        savedSettings = False

        waveheightLabel.Content = "Waveheight (Value: " & String.Format("{0:0.00}", waveheightSlider.Value) & "):"
    End Sub
End Class
